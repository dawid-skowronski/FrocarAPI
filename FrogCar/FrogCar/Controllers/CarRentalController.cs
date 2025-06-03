using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FrogCar.Data;
using FrogCar.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FrogCar.Constants;
using Microsoft.AspNetCore.Http;
using System;
using FrogCar.Controllers;
using FrogCar.Migrations;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CarRentalController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CarRentalController> _logger;

    public CarRentalController(AppDbContext context, INotificationService notificationService, ILogger<CarRentalController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException(ErrorMessages.Unauthorized));
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    private bool IsCurrentUserAdmin()
    {
        return GetCurrentUserRole() == Roles.Admin;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateCarRental([FromBody] CarRentalRequest carRentalRequest)
    {
        _logger.LogInformation("Rozpoczęto tworzenie nowego wypożyczenia samochodu.");

        if (carRentalRequest == null)
        {
            _logger.LogWarning("Brak danych wypożyczenia w żądaniu.");
            return BadRequest(new { message = ErrorMessages.BadRequestEmptyRental });
        }

        if (carRentalRequest.RentalEndDate <= carRentalRequest.RentalStartDate)
        {
            _logger.LogWarning("Data zakończenia wypożyczenia ({RentalEndDate}) jest wcześniejsza lub taka sama jak data rozpoczęcia ({RentalStartDate}).", carRentalRequest.RentalEndDate, carRentalRequest.RentalStartDate);
            return BadRequest(new { message = ErrorMessages.RentalEndDateBeforeStartDate });
        }

        var carListing = await _context.CarListing.FirstOrDefaultAsync(c => c.Id == carRentalRequest.CarListingId);
        if (carListing == null)
        {
            _logger.LogWarning("Próba wypożyczenia nieistniejącego samochodu o ID: {CarListingId}", carRentalRequest.CarListingId);
            return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.ListingNotFound });
        }

        if (!carListing.IsAvailable)
        {
            _logger.LogWarning("Próba wypożyczenia niedostępnego samochodu o ID: {CarListingId}", carRentalRequest.CarListingId);
            return BadRequest(new { message = ErrorMessages.CarNotAvailable });
        }

        if (!carListing.IsApproved)
        {
            _logger.LogWarning("Próba wypożyczenia niezatwierdzonego samochodu o ID: {CarListingId}", carRentalRequest.CarListingId);
            return BadRequest(new { message = ErrorMessages.CarNotApproved });
        }

        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogError("Użytkownik o ID {UserId} nie istnieje, mimo że token jest autoryzowany.", userId);
            return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.UserNotFound });
        }

        if (carListing.UserId == userId)
        {
            _logger.LogWarning("Użytkownik {UserId} próbował wypożyczyć własny samochód o ID: {CarListingId}", userId, carListing.Id);
            return BadRequest(new { message = ErrorMessages.CannotRentOwnCar });
        }

        var conflictingRental = await _context.CarRentals
            .AnyAsync(r => r.CarListingId == carRentalRequest.CarListingId &&
                           r.RentalStatus == "Aktywne" &&
                           (carRentalRequest.RentalStartDate < r.RentalEndDate &&
                            carRentalRequest.RentalEndDate > r.RentalStartDate));

        if (conflictingRental)
        {
            _logger.LogWarning("Samochód o ID {CarListingId} jest już wypożyczony w żądanym okresie.", carRentalRequest.CarListingId);
            return BadRequest(new { message = ErrorMessages.CarAlreadyRentedInPeriod });
        }

        var rentalDays = (carRentalRequest.RentalEndDate - carRentalRequest.RentalStartDate).Days;
        if (rentalDays < 1)
            rentalDays = 1;

        var rentalPrice = rentalDays * carListing.RentalPricePerDay;

        var carRental = new CarRental
        {
            CarListingId = carListing.Id,
            UserId = user.Id,
            RentalStartDate = carRentalRequest.RentalStartDate,
            RentalEndDate = carRentalRequest.RentalEndDate,
            RentalPrice = rentalPrice,
            RentalStatus = "Aktywne"
        };

        _context.CarRentals.Add(carRental);
        carListing.IsAvailable = false;
        _context.CarListing.Update(carListing);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nowe wypożyczenie o ID {CarRentalId} zostało utworzone dla użytkownika {UserId} dla samochodu {CarListingId}.", carRental.CarRentalId, userId, carListing.Id);

        await _notificationService.CreateNotificationAsync(
            carListing.UserId,
            null,
            $"Twój samochód ({carListing.Brand} {carListing.EngineCapacity}L) został wypożyczony przez {user.Username} na okres od {carRental.RentalStartDate.ToShortDateString()} do {carRental.RentalEndDate.ToShortDateString()}."
        );
        _logger.LogInformation("Powiadomienie o nowym wypożyczeniu wysłane do właściciela samochodu ID: {OwnerId}.", carListing.UserId);

        return Ok(new { message = "Wypożyczenie zostało dodane.", carRental });
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserCarRentals()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Pobieranie aktywnych wypożyczeń dla użytkownika o ID: {UserId}", userId);

        var rentals = await _context.CarRentals
            .Where(r => r.UserId == userId && r.RentalStatus == "Aktywne")
            .Include(r => r.CarListing)
            .Include(r => r.User)
            .ToListAsync();

        if (rentals == null || !rentals.Any())
        {
            _logger.LogInformation("Brak aktywnych wypożyczeń dla użytkownika o ID: {UserId}.", userId);
            return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.NoActiveRentalsForUser });
        }

        _logger.LogInformation("Pomyślnie pobrano {Count} aktywnych wypożyczeń dla użytkownika o ID: {UserId}.", rentals.Count, userId);
        return Ok(rentals);
    }

    [HttpGet("list")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAllCarRentals()
    {
        _logger.LogInformation("Pobieranie wszystkich wypożyczeń.");

        var rentals = await _context.CarRentals
            .Include(r => r.CarListing)
            .Include(r => r.User)
            .ToListAsync();

        if (rentals == null || !rentals.Any())
        {
            _logger.LogInformation("Brak wypożyczeń w systemie.");
            return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.NoRentalsFound });
        }

        _logger.LogInformation("Pomyślnie pobrano {Count} wszystkich wypożyczeń.", rentals.Count);
        return Ok(rentals);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCarRental(int id)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();
        _logger.LogInformation("Pobieranie wypożyczenia o ID: {CarRentalId} przez użytkownika ID: {UserId}", id, currentUserId);

        var rental = await _context.CarRentals
            .Include(r => r.CarListing)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.CarRentalId == id);

        if (rental == null)
        {
            _logger.LogWarning("Wypożyczenie o ID: {CarRentalId} nie istnieje.", id);
            return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.RentalNotFound });
        }

        var isOwner = rental.CarListing.UserId == currentUserId;
        var isRenter = rental.UserId == currentUserId;

        if (!isOwner && !isRenter && !IsCurrentUserAdmin())
        {
            _logger.LogWarning("Użytkownik {UserId} próbował uzyskać dostęp do wypożyczenia {CarRentalId} bez uprawnień.", currentUserId, id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ErrorMessages.NotOwnerRenterOrAdmin });
        }

        _logger.LogInformation("Pomyślnie pobrano wypożyczenie o ID: {CarRentalId}.", id);
        return Ok(rental);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateCarRentalStatus(int id, [FromBody] string status)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();
        _logger.LogInformation("Użytkownik ID: {UserId} próbuje zmienić status wypożyczenia ID: {CarRentalId} na: {NewStatus}", currentUserId, id, status);

        var rental = await _context.CarRentals
            .Include(r => r.CarListing)
            .FirstOrDefaultAsync(r => r.CarRentalId == id);

        if (rental == null)
        {
            _logger.LogWarning("Próba zmiany statusu nieistniejącego wypożyczenia o ID: {CarRentalId}.", id);
            return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.RentalNotFound });
        }

        if (rental.CarListing.UserId != currentUserId && !IsCurrentUserAdmin())
        {
            _logger.LogWarning("Użytkownik {UserId} próbował zmienić status wypożyczenia {CarRentalId}, do którego nie ma uprawnień.", currentUserId, id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ErrorMessages.NotOwnerOrAdminRentalStatus });
        }

        rental.RentalStatus = status;

        if (status == "Zakończone" || status == "Anulowane")
        {
            rental.CarListing.IsAvailable = true;
            _context.CarListing.Update(rental.CarListing);
            _logger.LogInformation("Dostępność samochodu ID: {CarListingId} ustawiono na 'true' po zmianie statusu wypożyczenia ID: {CarRentalId} na '{Status}'.", rental.CarListing.Id, id, status);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Status wypożyczenia ID: {CarRentalId} zmieniono na '{NewStatus}' przez użytkownika ID: {UserId}.", id, status, currentUserId);

        await _notificationService.CreateNotificationAsync(
            rental.UserId,
            null,
            $"Status Twojego wypożyczenia samochodu o ID: {rental.CarRentalId} został zmieniony na: {status}."
        );
        _logger.LogInformation("Powiadomienie o zmianie statusu wysłane do użytkownika wypożyczającego ID: {RenterId}.", rental.UserId);

        return Ok(new { message = "Status wypożyczenia został zmieniony.", rental });
    }

    [HttpGet("user/history")]
    public async Task<IActionResult> GetUserCarRentalHistory()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Pobieranie historii wypożyczeń dla użytkownika o ID: {UserId}", userId);

        var endedRentals = await _context.CarRentals
            .Where(r => r.UserId == userId && (r.RentalStatus == "Zakończone" || r.RentalStatus == "Anulowane"))
            .Include(r => r.CarListing)
            .Include(r => r.User)
            .ToListAsync();

        if (endedRentals == null || !endedRentals.Any())
        {
            _logger.LogInformation("Brak zakończonych/anulowanych wypożyczeń dla użytkownika o ID: {UserId}.", userId);
            return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.NoEndedRentalsForUser });
        }

        _logger.LogInformation("Pomyślnie pobrano {Count} zakończonych/anulowanych wypożyczeń dla użytkownika o ID: {UserId}.", endedRentals.Count, userId);
        return Ok(endedRentals);
    }

    [HttpPost("review")]
    public async Task<IActionResult> AddReview([FromBody] CarRentalReviewRequest reviewRequest)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Użytkownik ID: {UserId} próbuje dodać recenzję dla wypożyczenia ID: {CarRentalId}", userId, reviewRequest.CarRentalId);

        if (reviewRequest.Rating < 1 || reviewRequest.Rating > 5)
        {
            _logger.LogWarning("Nieprawidłowa ocena ({Rating}) dla recenzji wypożyczenia ID: {CarRentalId}.", reviewRequest.Rating, reviewRequest.CarRentalId);
            return BadRequest(new { message = ErrorMessages.InvalidRating });
        }

        var rental = await _context.CarRentals
            .FirstOrDefaultAsync(r => r.CarRentalId == reviewRequest.CarRentalId && r.UserId == userId);

        if (rental == null)
        {
            _logger.LogWarning("Nie znaleziono wypożyczenia ID: {CarRentalId} dla użytkownika ID: {UserId} w celu dodania recenzji.", reviewRequest.CarRentalId, userId);
            return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.RentalNotFound });
        }

        if (rental.RentalStatus != "Zakończone")
        {
            _logger.LogWarning("Próba dodania recenzji dla niezakończonego wypożyczenia ID: {CarRentalId}. Obecny status: {Status}", reviewRequest.CarRentalId, rental.RentalStatus);
            return BadRequest(new { message = ErrorMessages.ReviewForEndedRentalsOnly });
        }

        var existingReview = await _context.CarRentalReviews
            .FirstOrDefaultAsync(r => r.CarRentalId == reviewRequest.CarRentalId && r.UserId == userId);

        if (existingReview != null)
        {
            _logger.LogWarning("Użytkownik ID: {UserId} próbował dodać drugą recenzję dla wypożyczenia ID: {CarRentalId}.", userId, reviewRequest.CarRentalId);
            return BadRequest(new { message = ErrorMessages.AlreadyReviewed });
        }

        var review = new CarRentalReview
        {
            CarRentalId = reviewRequest.CarRentalId,
            UserId = userId,
            Rating = reviewRequest.Rating,
            Comment = reviewRequest.Comment
        };

        _context.CarRentalReviews.Add(review);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Recenzja o ID: {ReviewId} dodana dla wypożyczenia ID: {CarRentalId} przez użytkownika ID: {UserId}.", review.ReviewId, reviewRequest.CarRentalId, userId);

        var carListingId = rental.CarListingId;
        var averageRating = await _context.CarRentalReviews
            .Where(r => r.CarRental.CarListingId == carListingId)
            .AverageAsync(r => (double?)r.Rating) ?? 0;

        var listing = await _context.CarListing.FindAsync(carListingId);
        if (listing != null)
        {
            listing.AverageRating = Math.Round(averageRating, 2);
            _context.CarListing.Update(listing);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Średnia ocena dla ogłoszenia ID: {CarListingId} zaktualizowana do: {AverageRating}.", carListingId, listing.AverageRating);
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
        _logger.LogInformation("Pobieranie recenzji dla ogłoszenia ID: {ListingId}", listingId);

        var reviews = await _context.CarRentalReviews
            .Include(r => r.User)
            .Include(r => r.CarRental)
            .Where(r => r.CarRental.CarListingId == listingId)
            .ToListAsync();

        if (reviews == null || !reviews.Any())
        {
            _logger.LogInformation("Brak recenzji dla ogłoszenia ID: {ListingId}.", listingId);
            return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.NoReviewsForListing });
        }

        _logger.LogInformation("Pomyślnie pobrano {Count} recenzji dla ogłoszenia ID: {ListingId}.", reviews.Count, listingId);
        return Ok(reviews);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCarRental(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Użytkownik ID: {UserId} próbuje usunąć wypożyczenie ID: {CarRentalId}", userId, id);

        var rental = await _context.CarRentals
            .Include(r => r.CarListing)
            .FirstOrDefaultAsync(r => r.CarRentalId == id);

        if (rental == null)
        {
            _logger.LogWarning("Próba usunięcia nieistniejącego wypożyczenia o ID: {CarRentalId}.", id);
            return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.RentalNotFound });
        }

        if (rental.CarListing.UserId != userId && !IsCurrentUserAdmin())
        {
            _logger.LogWarning("Użytkownik ID: {UserId} próbował usunąć wypożyczenie ID: {CarRentalId}, do którego nie ma uprawnień.", userId, id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ErrorMessages.NotOwnerOrAdminRentalDeletion });
        }

        if (rental.RentalStatus == "Aktywne")
        {
            rental.RentalStatus = "Anulowane";
            rental.CarListing.IsAvailable = true;
            _context.CarListing.Update(rental.CarListing);
            _logger.LogInformation("Aktywne wypożyczenie ID: {CarRentalId} anulowano, a samochód ID: {CarListingId} stał się ponownie dostępny.", id, rental.CarListing.Id);
        }
        else
        {
            _context.CarRentals.Remove(rental);
            _logger.LogInformation("Wypożyczenie ID: {CarRentalId} (status: {Status}) zostało całkowicie usunięte.", id, rental.RentalStatus);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Operacja usunięcia/anulowania wypożyczenia ID: {CarRentalId} zakończona pomyślnie.", id);
        return Ok("Wypożyczenie zostało usunięte i samochód jest teraz dostępny.");
    }

    private async Task SendRentalEndedNotification(CarRental rental)
    {
        var user = await _context.Users.FindAsync(rental.UserId);
        if (user == null)
        {
            _logger.LogWarning("Nie można wysłać powiadomienia o zakończeniu wypożyczenia ID: {RentalId}. Użytkownik {UserId} nie istnieje.", rental.CarRentalId, rental.UserId);
            return;
        }

        var notificationMessage = $"Twoje wypożyczenie samochodu o ID {rental.CarRentalId} zostało zakończone.";

        await _notificationService.CreateNotificationAsync(
            user.Id,
            null,
            notificationMessage
        );
        _logger.LogInformation("Powiadomienie o zakończeniu wypożyczenia ID: {RentalId} wysłane do użytkownika ID: {UserId}.", rental.CarRentalId, user.Id);
    }
}