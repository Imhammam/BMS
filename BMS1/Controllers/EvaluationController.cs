using BMS1.Data;
using BMS1.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BMS1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvaluationController : Controller
    {
        private readonly ApplicationDbContext _db;
        public EvaluationController(ApplicationDbContext db)
        {
            _db = db;
        }

        [Authorize(Roles = "Evaluator")]
        [HttpGet("auto-evaluate/{tenderId}")]
        public async Task<IActionResult> AutoEvaluate(Guid tenderId)
        {
            var bids = await _db.Bids
                .Where(b => b.TenderId == tenderId)
                .OrderBy(b => b.Price)
                .ToListAsync();

            if (!bids.Any())
                return NotFound("No bids for this tender");

            var topBid = bids.First();
            return Ok(new
            {
                BidId = topBid.Id,
                Price = topBid.Price,
                Summary = topBid.ProposalSummary
            });
        }

        [Authorize(Roles = "Evaluator")]
        [HttpPost("award")]
        public async Task<IActionResult> SelectWinner(EvaluationResultDTO dto)
        {
            var bid = await _db.Bids.Include(b => b.Tender).FirstOrDefaultAsync(b => b.Id == dto.BidId);
            if (bid == null)
                return NotFound("Bid not found");

            var tender = bid.Tender!;
            tender.WinningBidId = dto.BidId;

            await _db.SaveChangesAsync();
            return Ok(new
            {
                Message = "Bid awarded successfully",
                Winner = bid.BidderId,
                BidAmount = bid.Price
            });
        }

        // 🟢 عرض الفائز في عطاء معيّن
        [HttpGet("winner/{tenderId}")]
        public async Task<IActionResult> GetWinner(Guid tenderId)
        {
            var tender = await _db.Tenders
                .Include(t => t.WinningBid)
                .ThenInclude(b => b!.Bidder)
                .FirstOrDefaultAsync(t => t.Id == tenderId);

            if (tender == null || tender.WinningBid == null)
                return NotFound("No winner selected yet");

            return Ok(new
            {
                WinnerName = tender.WinningBid.Bidder!.FullName,
                Price = tender.WinningBid.Price,
                Summary = tender.WinningBid.ProposalSummary
            });
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
