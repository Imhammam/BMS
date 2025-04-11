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
    public class TenderController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TenderController(ApplicationDbContext db)
        {
            _db = db;
        }

        [Authorize(Roles = "Officer")]
        [HttpPost]
        public async Task<IActionResult> CreateTender([FromBody] TenderDTO dto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("User email not found");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tender = new Tender
            {
                Title = dto.Title,
                Description = dto.Description,
                Budget = dto.Budget,
                Deadline = dto.Deadline,
                Category = dto.Category,
                Type = dto.Type,
                Location = dto.Location,
                CreatedByEmail = email
            };

            await _db.Tenders.AddAsync(tender);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTender), new { id = tender.Id }, tender);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOpen()
        {
            var tenders = await _db.Tenders
                .Where(t => t.Deadline > DateTime.UtcNow)
                .OrderByDescending(t => t.Deadline)
                .ToListAsync();

            return Ok(tenders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTender(Guid id)
        {
            var tender = await _db.Tenders.FindAsync(id);

            if (tender == null)
            {
                return NotFound();
            }

            return Ok(tender);
        }

        [Authorize(Roles = "Officer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTender(Guid id, [FromBody] TenderDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tender = await _db.Tenders.FindAsync(id);

            if (tender == null)
            {
                return NotFound();
            }

            tender.Title = dto.Title;
            tender.Description = dto.Description;
            tender.Budget = dto.Budget;
            tender.Deadline = dto.Deadline;
            tender.Category = dto.Category;
            tender.Type = dto.Type;
            tender.Location = dto.Location;

            await _db.SaveChangesAsync();

            return Ok(tender);
        }

        [Authorize(Roles = "Officer")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTender(Guid id)
        {
            var tender = await _db.Tenders.FindAsync(id);

            if (tender == null)
            {
                return NotFound();
            }

            _db.Tenders.Remove(tender);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

   
}