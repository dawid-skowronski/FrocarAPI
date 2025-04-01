using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FrogCar.Data;
using FrogCar.Models;
using System;

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
    public async Task<IActionResult> GetAllCarListings(double? lat, double? lng, double radius = 50)
    {
        var listings = await _context.CarListing.ToListAsync();

        if (listings == null || listings.Count == 0)
            return NotFound("Brak ogłoszeń.");

        // Jeśli podano współrzędne, filtruj według odległości
        if (lat.HasValue && lng.HasValue)
        {
            listings = listings.Where(listing =>
                CalculateDistance(lat.Value, lng.Value, listing.Latitude, listing.Longitude) <= radius)
                .ToList();

            if (listings.Count == 0)
                return NotFound("Brak dostępnych samochodów w podanym regionie.");
        }

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

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCarListing(int id, [FromBody] CarListing updatedListing)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var listing = await _context.CarListing.FindAsync(id);

        if (listing == null)
            return NotFound("Ogłoszenie nie istnieje.");

        if (listing.UserId != userId)
            return BadRequest("To nie jest Twoje ogłoszenie. Tylko właściciel może je edytować.");

        // Walidacja danych
        if (string.IsNullOrEmpty(updatedListing.Brand))
            return BadRequest("Marka samochodu jest wymagana.");
        if (updatedListing.EngineCapacity <= 0)
            return BadRequest("Pojemność silnika musi być większa od 0.");
        if (updatedListing.Seats <= 0)
            return BadRequest("Liczba miejsc musi być większa od 0.");
        if (string.IsNullOrEmpty(updatedListing.FuelType))
            return BadRequest("Typ paliwa jest wymagany.");
        if (string.IsNullOrEmpty(updatedListing.CarType))
            return BadRequest("Typ samochodu jest wymagany.");
        if (updatedListing.Features != null && updatedListing.Features.Any(f => string.IsNullOrEmpty(f)))
            return BadRequest("Każda cecha musi być wypełniona poprawnie.");
        if (updatedListing.RentalPricePerDay <= 0)
            return BadRequest("Cena wynajmu na jeden dzień musi być większa niż 0.");

        // Aktualizacja pól
        listing.Brand = updatedListing.Brand;
        listing.EngineCapacity = updatedListing.EngineCapacity;
        listing.FuelType = updatedListing.FuelType;
        listing.Seats = updatedListing.Seats;
        listing.CarType = updatedListing.CarType;
        listing.Features = updatedListing.Features;
        listing.Latitude = updatedListing.Latitude;
        listing.Longitude = updatedListing.Longitude;
        listing.RentalPricePerDay = updatedListing.RentalPricePerDay;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Ogłoszenie zostało zaktualizowane.", listing });
    }

    // Funkcja obliczająca odległość między dwoma punktami (wzór Haversine)
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Promień Ziemi w kilometrach
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = R * c; // Odległość w kilometrach

        return distance;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}