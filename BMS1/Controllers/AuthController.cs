using BMS1.Data;
using BMS1.Interfaces;
using BMS1.Models.DTO;
using BMS1.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BMS1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext db,
            ITokenService tokenService,
            ILogger<AuthController> logger)
        {
            _db = db;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower().Trim()))
                    return Conflict(new { Message = "Email already exists" });

                using var hmac = new HMACSHA256();
                var salt = Convert.ToBase64String(hmac.Key);
                var hash = Convert.ToBase64String(
                    hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password + salt)));

                var user = new User
                {
                    FullName = dto.FullName.Trim(),
                    Email = dto.Email.ToLower().Trim(),
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    CreatedAt = DateTime.UtcNow
                };

                await _db.Users.AddAsync(user);
                await _db.SaveChangesAsync();

                var token = _tokenService.CreateToken(user);

                _logger.LogInformation($"New user registered: {user.Email}");

                return Ok(new
                {
                    Token = token,
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    ExpiresIn = "24h"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower().Trim());

                if (user == null)
                    return Unauthorized(new { Message = "Invalid credentials" });

                using var hmac = new HMACSHA256(Convert.FromBase64String(user.PasswordSalt));
                var computedHash = Convert.ToBase64String(
                    hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password + user.PasswordSalt)));

                if (computedHash != user.PasswordHash)
                    return Unauthorized(new { Message = "Invalid credentials" });

                var token = _tokenService.CreateToken(user);

                _logger.LogInformation($"User logged in: {user.Email}");

                return Ok(new
                {
                    Token = token,
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    ExpiresIn = "24h"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }
    }
}