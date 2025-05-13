using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FrogCar.Data;
using FrogCar.Models;
using System.Linq;
using System.Threading.Tasks;
using FrogCar.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CarRentalController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public CarRentalController(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
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

        if (!carListing.IsApproved)
            return BadRequest("Samochód jest nie dostępny.");

        carListing.IsAvailable = false;

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("Użytkownik nie istnieje.");
        if (carListing.UserId == userId)
            return BadRequest("Nie możesz wypożyczyć własnego samochodu.");

        var rentalDays = (carRentalRequest.RentalEndDate - carRentalRequest.RentalStartDate).Days;

        if (rentalDays < 1)
            rentalDays = 1;

        var rentalPrice = rentalDays * carListing.RentalPricePerDay;

        var carRental = new CarRental
        {
            CarListingId = carListing.Id,
            CarListing = carListing,
            UserId = user.Id,
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
            .Where(r => r.UserId == userId && r.RentalStatus != "Ended")
            .Include(r => r.CarListing)
            .Include(r => r.User)
            .ToListAsync();

        if (rentals == null || rentals.Count == 0)
            return NotFound("Brak aktywnych wypożyczeń dla tego użytkownika.");

        return Ok(rentals);
    }


    [HttpGet("list")]
    public async Task<IActionResult> GetAllCarRentals()
    {
        var rentals = await _context.CarRentals
            .Include(r => r.CarListing)
             .Include(r => r.User)
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
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var rental = await _context.CarRentals.FindAsync(id);

        if (rental == null)
            return NotFound("Wypożyczenie nie istnieje.");

        if (rental.UserId != userId && userRole != "Admin")
            return BadRequest("To nie jest Twoje wypożyczenie. Tylko właściciel lub administrator może zmieniać status.");

        rental.RentalStatus = status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status wypożyczenia został zmieniony.", rental });
    }

    [HttpGet("user/history")]
    public async Task<IActionResult> GetUserCarRentalHistory()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var endedRentals = await _context.CarRentals
            .Where(r => r.UserId == userId && r.RentalStatus == "Ended")
            .Include(r => r.CarListing)
            .Include(r => r.User)
            .ToListAsync();

        if (endedRentals == null || endedRentals.Count == 0)
            return NotFound("Brak zakończonych wypożyczeń dla tego użytkownika.");

        return Ok(endedRentals);
    }

    [HttpPost("review")]
    public async Task<IActionResult> AddReview([FromBody] CarRentalReviewRequest reviewRequest)
    {
        if (reviewRequest.Rating < 1 || reviewRequest.Rating > 5)
            return BadRequest("Ocena musi być w zakresie 1-5.");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var rental = await _context.CarRentals
            .FirstOrDefaultAsync(r => r.CarRentalId == reviewRequest.CarRentalId && r.UserId == userId);

        if (rental == null)
            return NotFound("Nie znaleziono wypożyczenia.");

        if (rental.RentalStatus != "Ended")
            return BadRequest("Recenzja może być wystawiona tylko dla zakończonych wypożyczeń.");

        var existingReview = await _context.CarRentalReviews
    .FirstOrDefaultAsync(r => r.CarRentalId == reviewRequest.CarRentalId && r.UserId == userId);

        if (existingReview != null)
            return BadRequest("Wystawiłeś już recenzję dla tego wypożyczenia.");


        var review = new CarRentalReview
        {
            CarRentalId = reviewRequest.CarRentalId,
            UserId = userId,
            Rating = reviewRequest.Rating,
            Comment = reviewRequest.Comment
        };

        _context.CarRentalReviews.Add(review);
        await _context.SaveChangesAsync();

        var carListingId = rental.CarListingId;

        var averageRating = await _context.CarRentalReviews
            .Where(r => r.CarRental.CarListingId == carListingId)
            .AverageAsync(r => (double?)r.Rating) ?? 0;

        var listing = await _context.CarListing.FindAsync(carListingId);
        if (listing != null)
        {
            listing.AverageRating = Math.Round(averageRating, 2);
            await _context.SaveChangesAsync();
        }

        var addedReview = await _context.CarRentalReviews
            .Include(r => r.User)
            .Include(r => r.CarRental)
            .ThenInclude(cr => cr.CarListing)
            .FirstOrDefaultAsync(r => r.ReviewId == review.ReviewId);

        return Ok(new { message = "Recenzja została dodana.", review = addedReview });
    }



    [HttpGet("reviews/{listingId}")]
    public async Task<IActionResult> GetReviewsForListing(int listingId)
    {
        var reviews = await _context.CarRentalReviews
            .Include(r => r.User)
            .Include(r => r.CarRental)
            .Where(r => r.CarRental.CarListingId == listingId)
            .ToListAsync();

        if (reviews == null || reviews.Count == 0)
            return NotFound("Brak recenzji dla tego ogłoszenia.");

        return Ok(reviews);
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

        if (rental.UserId != userId && userRole != "Admin")
            return Unauthorized(new { message = "Nie masz uprawnień do usunięcia tego wypożyczenia." });

        rental.CarListing.IsAvailable = true;

        _context.CarRentals.Remove(rental);
        _context.CarListing.Update(rental.CarListing);
        await _context.SaveChangesAsync();

        return Ok("Wypożyczenie zostało usunięte i samochód jest teraz dostępny.");
    }

    private async Task SendRentalEndedNotification(CarRental rental)
    {
        var user = await _context.Users.FindAsync(rental.UserId);
        if (user == null) return;

        var notificationMessage = $"Twoje wypożyczenie samochodu o ID {rental.CarRentalId} zostało zakończone.";

        var notification = new Notification
        {
            UserId = user.Id,
            Message = notificationMessage,
            CreatedAt = DateTime.UtcNow,
            IsRead = false 
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }



}
