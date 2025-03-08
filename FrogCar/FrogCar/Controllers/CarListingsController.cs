using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FrogCar.Data;
using FrogCar.Models;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CarListingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CarListingsController(AppDbContext context)
    {
        _context = context;
    }

    // Dodawanie nowego ogłoszenia
    [HttpPost("create")]
    public async Task<IActionResult> AddCarListing([FromBody] CarListing carListing)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        carListing.UserId = userId;

        _context.CarListing.Add(carListing);
        await _context.SaveChangesAsync();

        return Ok(carListing);
    }

    // Pobieranie ogłoszeń użytkownika
    [HttpGet("user")]
    public async Task<IActionResult> GetUserCarListings()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var listings = await _context.CarListing.Where(l => l.UserId == userId).ToListAsync();

        return Ok(listings);
    }

    // Pobieranie wszystkich ogłoszeń
    [HttpGet("List")]
    public async Task<IActionResult> GetAllCarListings()
    {
        var listings = await _context.CarListing.ToListAsync();
        return Ok(listings);
    }

    // Pobieranie szczegółów konkretnego ogłoszenia
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCarListing(int id)
    {
        var listing = await _context.CarListing.FindAsync(id);
        if (listing == null) return NotFound("Ogłoszenie nie istnieje.");

        return Ok(listing);
    }

    // Usuwanie ogłoszenia
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCarListing(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var listing = await _context.CarListing.FindAsync(id);

        if (listing == null) return NotFound("Ogłoszenie nie istnieje.");
        if (listing.UserId != userId) return Forbid();

        _context.CarListing.Remove(listing);
        await _context.SaveChangesAsync();

        return Ok("Ogłoszenie usunięte.");
    }
}