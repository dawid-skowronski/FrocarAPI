﻿using Microsoft.AspNetCore.Authorization;
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
        carListing.IsApproved = false;

        _context.CarListing.Add(carListing);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Ogłoszenie zostało dodane poprawnie, i oczekuje na zatwierdzenie przez Administratora", carListing });
    }

    [HttpPut("{id}/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveListing(int id)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole != "Admin")
        {
            return BadRequest("Brak uprawnień do zatwierdzenia ogłoszenia. Tylko administrator może to zrobić.");
        }

        var listing = await _context.CarListing.FindAsync(id);

        if (listing == null)
            return NotFound("Ogłoszenie nie istnieje.");

        listing.IsApproved = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Ogłoszenie zostało zatwierdzone.", listing });
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
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var listings = await _context.CarListing
            .Where(l => l.IsApproved == true && l.UserId != userId)
            .ToListAsync();

        if (listings == null || listings.Count == 0)
            return NotFound("Brak ogłoszeń.");

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
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var listing = await _context.CarListing.FindAsync(id);

        if (listing == null)
            return NotFound("Ogłoszenie nie istnieje.");

        if (listing.UserId != userId && userRole != "Admin")
            return Forbid("Nie masz uprawnień do usunięcia tego ogłoszenia.");

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

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; 
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = R * c; 

        return distance;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}