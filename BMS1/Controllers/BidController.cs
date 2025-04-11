using BMS1.Data;
using BMS1.Models.DTO;
using BMS1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BMS1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BidController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BidController> _logger;

        public BidController(ApplicationDbContext db, ILogger<BidController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [Authorize(Roles = "Bidder")]
        [HttpPost("{tenderId}")]
        public async Task<IActionResult> SubmitBid(
            [FromRoute] Guid tenderId,
            [FromBody] BidDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var email = User.FindFirstValue(ClaimTypes.Email);
                var bidder = await _db.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (bidder == null)
                    return Unauthorized(new { Message = "User not found" });

                var tender = await _db.Tenders
                    .FirstOrDefaultAsync(t => t.Id == tenderId);

                if (tender == null)
                    return NotFound(new { Message = "Tender not found" });

                if (tender.Deadline <= DateTime.UtcNow)
                    return BadRequest(new { Message = "Bidding period has ended" });

                bool alreadyBid = await _db.Bids
                    .AnyAsync(b => b.TenderId == tenderId && b.BidderId == bidder.Id);

                if (alreadyBid)
                    return Conflict(new { Message = "You already submitted a bid for this tender" });

                var bid = new Bid
                {
                    TenderId = tenderId,
                    BidderId = bidder.Id,  
                    ProposalSummary = dto.ProposalSummary?.Trim() ?? string.Empty,
                    Price = dto.Price,
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Submitted"  
                };

                await _db.Bids.AddAsync(bid);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"New bid submitted by {email} for tender {tenderId}");

                return CreatedAtAction(
                    nameof(GetBidDetails),
                    new { id = bid.Id },
                    new
                    {
                        bid.Id,
                        bid.TenderId,
                        bid.Price,
                        SubmittedAt = bid.SubmittedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                        bid.Status
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting bid");
                return StatusCode(500, new { Message = "An error occurred while processing your request" });
            }
        }

        [Authorize(Roles = "Bidder")]
        [HttpGet("my-bids")]
        public async Task<IActionResult> GetMyBids()
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                var bidder = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (bidder == null)
                    return Unauthorized(new { Message = "User not found" });

                var bids = await _db.Bids
                    .Where(b => b.BidderId == bidder.Id)
                    .Include(b => b.Tender)
                    .Select(b => new {
                        b.Id,
                        TenderTitle = b.Tender != null ? b.Tender.Title : string.Empty,
                        b.Price,
                        SubmittedAt = b.SubmittedAt,
                        b.Status,
                        DaysRemaining = b.Tender != null ? (b.Tender.Deadline - DateTime.UtcNow).Days : 0
                    })
                    .ToListAsync();

                return Ok(bids);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bids");
                return StatusCode(500, new { Message = "An error occurred while retrieving bids" });
            }
        }

        [Authorize(Roles = "Officer")]
        [HttpGet("tender/{tenderId}")]
        public async Task<IActionResult> GetBidsForTender([FromRoute] Guid tenderId)
        {
            try
            {
                var bids = await _db.Bids
                    .Where(b => b.TenderId == tenderId)
                    .Include(b => b.Bidder)
                    .Select(b => new {
                        b.Id,
                        BidderName = b.Bidder != null ? b.Bidder.FullName : string.Empty,
                        b.Price,
                        SubmittedAt = b.SubmittedAt,
                        b.ProposalSummary,
                        b.Status
                    })
                    .OrderBy(b => b.Price)
                    .ToListAsync();

                if (!bids.Any())
                    return NotFound(new { Message = "No bids found for this tender" });

                return Ok(bids);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting bids for tender {tenderId}");
                return StatusCode(500, new { Message = "An error occurred while retrieving bids" });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBidDetails(Guid id)
        {
            try
            {
                var bid = await _db.Bids
                    .Include(b => b.Tender)
                    .Include(b => b.Bidder)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (bid == null)
                    return NotFound(new { Message = "Bid not found" });

                // Authorization check
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (User.IsInRole("Bidder") && (bid.Bidder == null || bid.Bidder.Email != email))
                    return Forbid();

                return Ok(new
                {
                    bid.Id,
                    TenderTitle = bid.Tender != null ? bid.Tender.Title : string.Empty,
                    BidderName = bid.Bidder != null ? bid.Bidder.FullName : string.Empty,
                    bid.Price,
                    bid.ProposalSummary,
                    SubmittedAt = bid.SubmittedAt,
                    bid.Status,
                    TenderDeadline = bid.Tender != null ? bid.Tender.Deadline : DateTime.MinValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting bid details {id}");
                return StatusCode(500, new { Message = "An error occurred while retrieving bid details" });
            }
        }

        
    }
}