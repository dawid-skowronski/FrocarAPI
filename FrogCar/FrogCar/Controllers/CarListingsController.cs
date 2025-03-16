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

    [HttpPost("create")]
    public async Task<IActionResult> AddCarListing([FromBody] CarListing carListing)
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Musisz być zalogowany, aby dodać ogłoszenie." });


        if (!User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Musisz być zalogowany, aby dodać ogłoszenie." });

        if (carListing == null)
            return BadRequest("Ogłoszenie nie może być puste.");

        if (string.IsNullOrEmpty(carListing.Brand))
            return BadRequest("Marka samochodu jest wymagana.");

        if (carListing.EngineCapacity <= 0)
            return BadRequest("Pojemność silnika musi być większa od 0.");

        if (carListing.Seats <= 0)
            return BadRequest("Liczba miejsc musi być większa od 0.");

        if (string.IsNullOrEmpty(carListing.FuelType))
            return BadRequest("Typ paliwa jest wymagany.");

        if (string.IsNullOrEmpty(carListing.CarType))
            return BadRequest("Typ samochodu jest wymagany.");

        if (carListing.Features != null && carListing.Features.Any(f => string.IsNullOrEmpty(f)))
            return BadRequest("Każda cecha musi być wypełniona poprawnie.");

        if (carListing.RentalPricePerDay <= 0)
            return BadRequest("Cena wynajmu na jeden dzień musi być większa niż 0.");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        carListing.UserId = userId;
        carListing.IsAvailable = true; 

        _context.CarListing.Add(carListing);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Ogłoszenie zostało dodane poprawnie.", carListing });
    }

    [HttpPut("{id}/availability")]
    public async Task<IActionResult> UpdateCarAvailability(int id, [FromBody] bool isAvailable)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var listing = await _context.CarListing.FindAsync(id);

        if (listing == null)
            return NotFound("Ogłoszenie nie istnieje.");

        if (listing.UserId != userId)
            return BadRequest("To nie jest Twoje ogłoszenie. Tylko właściciel może zmieniać dostępność.");

        listing.IsAvailable = isAvailable;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status dostępności zmieniony.", listing });
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserCarListings()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var listings = await _context.CarListing.Where(l => l.UserId == userId).ToListAsync();

        if (listings == null || listings.Count == 0)
            return NotFound("Brak ogłoszeń dla tego użytkownika.");

        return Ok(listings);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetAllCarListings()
    {
        var listings = await _context.CarListing.ToListAsync();

        if (listings == null || listings.Count == 0)
            return NotFound("Brak ogłoszeń.");

        return Ok(listings);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCarListing(int id)
    {
        var listing = await _context.CarListing.FindAsync(id);
        if (listing == null)
            return NotFound("Ogłoszenie nie istnieje.");

        return Ok(listing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCarListing(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var listing = await _context.CarListing.FindAsync(id);

        if (listing == null)
            return NotFound("Ogłoszenie nie istnieje.");

        if (listing.UserId != userId)
            return BadRequest("To nie jest Twoje ogłoszenie. Tylko właściciel może usunąć ogłoszenie.");

        _context.CarListing.Remove(listing);
        await _context.SaveChangesAsync();

        return Ok("Ogłoszenie usunięte.");
    }
}

