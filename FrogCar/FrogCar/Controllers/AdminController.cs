using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using FrogCar.Data;
using FrogCar.Models;
using Microsoft.AspNetCore.Identity;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    private bool IsAdmin()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return role == "Admin";
    }

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        var users = _context.Users
            .Select(u => new { u.Id, u.Username, u.Email, u.Role })
            .ToList();

        return Ok(users);
    }

    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound(new { message = "Użytkownik o podanym ID nie istnieje." });

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email
        });
    }

    [HttpGet("listings")]
    public async Task<IActionResult> GetAllListings()
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

        var listings = await _context.CarListing.Include(c => c.User).ToListAsync();
        return Ok(listings);
    }


    [HttpDelete("user/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("Użytkownik nie istnieje.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok("Użytkownik został usunięty.");
    }


    [HttpPut("user/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserModel model)
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("Użytkownik nie istnieje.");

        if (!string.IsNullOrWhiteSpace(model.Username))
            user.Username = model.Username;

        if (!string.IsNullOrWhiteSpace(model.Email))
            user.Email = model.Email;


        if (!string.IsNullOrWhiteSpace(model.Password))
            user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password); 

        await _context.SaveChangesAsync();

        return Ok("Użytkownik został zaktualizowany.");
    }




    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}