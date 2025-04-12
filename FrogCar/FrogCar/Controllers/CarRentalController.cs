using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FrogCar.Data;
using FrogCar.Models;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CarRentalController : ControllerBase
{
    private readonly AppDbContext _context;

    public CarRentalController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateCarRental([FromBody] CarRentalRequest carRentalRequest)
    {
        if (carRentalRequest == null)
            return BadRequest("Dane wypożyczenia są wymagane.");

        if (carRentalRequest.RentalEndDate <= carRentalRequest.RentalStartDate)
            return BadRequest("Data zakończenia wypożyczenia musi być późniejsza niż data rozpoczęcia.");

        var carListing = await _context.CarListing.FirstOrDefaultAsync(c => c.Id == carRentalRequest.CarListingId);
        if (carListing == null)
            return NotFound("Samochód nie istnieje.");

        if (!carListing.IsAvailable)
            return BadRequest("Samochód jest już niedostępny.");

        carListing.IsAvailable = false;

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("Użytkownik nie istnieje.");

        var rentalDays = (carRentalRequest.RentalEndDate - carRentalRequest.RentalStartDate).Days;
        var rentalPrice = rentalDays * carListing.RentalPricePerDay;

        var carRental = new CarRental
        {
            CarListingId = carListing.Id,
            CarListing = carListing,
            UserId = user.Id,
            User = user,
            RentalStartDate = carRentalRequest.RentalStartDate,
            RentalEndDate = carRentalRequest.RentalEndDate,
            RentalPrice = rentalPrice,
            RentalStatus = "Active"
        };

        _context.CarRentals.Add(carRental);
        _context.CarListing.Update(carListing);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Wypożyczenie zostało dodane.", carRental });
    }



    [HttpGet("user")]
    public async Task<IActionResult> GetUserCarRentals()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var rentals = await _context.CarRentals
            .Where(r => r.UserId == userId)
            .Include(r => r.CarListing)
            .ToListAsync();

        if (rentals == null || rentals.Count == 0)
            return NotFound("Brak wypożyczeń dla tego użytkownika.");

        return Ok(rentals);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetAllCarRentals()
    {
        var rentals = await _context.CarRentals
            .Include(r => r.CarListing)
            .ToListAsync();

        if (rentals == null || rentals.Count == 0)
            return NotFound("Brak wypożyczeń.");

        return Ok(rentals);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCarRental(int id)
    {
        var rental = await _context.CarRentals
            .Include(r => r.CarListing)
            .FirstOrDefaultAsync(r => r.CarRentalId == id);

        if (rental == null)
            return NotFound("Wypożyczenie nie istnieje.");

        return Ok(rental);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateCarRentalStatus(int id, [FromBody] string status)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var rental = await _context.CarRentals.FindAsync(id);

        if (rental == null)
            return NotFound("Wypożyczenie nie istnieje.");

        if (rental.UserId != userId)
            return BadRequest("To nie jest Twoje wypożyczenie. Tylko właściciel może zmieniać status.");

        rental.RentalStatus = status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status wypożyczenia został zmieniony.", rental });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCarRental(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var rental = await _context.CarRentals
                                   .Include(r => r.CarListing)
                                   .FirstOrDefaultAsync(r => r.CarRentalId == id);

        if (rental == null)
            return NotFound("Wypożyczenie nie istnieje.");

        // Sprawdzenie: tylko właściciel lub admin
        if (rental.UserId != userId && userRole != "Admin")
            return Unauthorized(new { message = "Nie masz uprawnień do usunięcia tego wypożyczenia." });

        rental.CarListing.IsAvailable = true;

        _context.CarRentals.Remove(rental);
        _context.CarListing.Update(rental.CarListing);
        await _context.SaveChangesAsync();

        return Ok("Wypożyczenie zostało usunięte i samochód jest teraz dostępny.");
    }


}
