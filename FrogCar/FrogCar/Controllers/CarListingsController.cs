using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FrogCar.Data;
using FrogCar.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FrogCar.Constants;


namespace FrogCar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CarListingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CarListingsController> _logger;

        public CarListingsController(AppDbContext context, INotificationService notificationService, ILogger<CarListingsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("User ID not found in claims."));
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        private bool IsCurrentUserAdmin()
        {
            return GetCurrentUserRole() == Roles.Admin;
        }

        private string ValidateCarListing(CarListing carListing)
        {
            if (carListing == null)
                return ErrorMessages.BadRequestEmptyListing;
            if (string.IsNullOrEmpty(carListing.Brand))
                return ErrorMessages.BadRequestRequiredBrand;
            if (carListing.EngineCapacity <= 0)
                return ErrorMessages.BadRequestEngineCapacity;
            if (carListing.Seats <= 0)
                return ErrorMessages.BadRequestSeats;
            if (string.IsNullOrEmpty(carListing.FuelType))
                return ErrorMessages.BadRequestFuelType;
            if (string.IsNullOrEmpty(carListing.CarType))
                return ErrorMessages.BadRequestCarType;
            if (carListing.Features != null && carListing.Features.Any(f => string.IsNullOrEmpty(f)))
                return ErrorMessages.BadRequestInvalidFeature;
            if (carListing.RentalPricePerDay <= 0)
                return ErrorMessages.BadRequestRentalPrice;

            return null;
        }

        [HttpPost("create")]
        public async Task<IActionResult> AddCarListing([FromBody] CarListing carListing)
        {
            _logger.LogInformation("Rozpoczynanie dodawania nowego ogłoszenia przez użytkownika ID: {UserId}", GetCurrentUserId());

            var validationMessage = ValidateCarListing(carListing);
            if (validationMessage != null)
            {
                _logger.LogWarning("Niepoprawne dane ogłoszenia: {Message}", validationMessage);
                return BadRequest(new { message = validationMessage });
            }

            carListing.UserId = GetCurrentUserId();
            carListing.IsAvailable = true;
            carListing.IsApproved = false;

            _context.CarListing.Add(carListing);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ogłoszenie ID: {CarListingId} zostało dodane przez użytkownika ID: {UserId} i oczekuje na zatwierdzenie.", carListing.Id, carListing.UserId);

            await NotifyAdminsAboutNewListing(carListing.UserId);

            return Ok(new { message = "Ogłoszenie zostało dodane i oczekuje na zatwierdzenie.", id = carListing.Id });
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> ApproveListing(int id)
        {
            _logger.LogInformation("Admin ID: {AdminId} próbuje zatwierdzić ogłoszenie ID: {CarListingId}", GetCurrentUserId(), id);

            var listing = await _context.CarListing.FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                _logger.LogWarning("Admin próbował zatwierdzić nieistniejące ogłoszenie ID: {CarListingId}", id);
                return NotFound(new { message = ErrorMessages.ListingNotFound });
            }

            if (listing.IsApproved)
            {
                _logger.LogWarning("Admin próbował zatwierdzić już zatwierdzone ogłoszenie ID: {CarListingId}", id);
                return BadRequest(new { message = "Ogłoszenie jest już zatwierdzone." });
            }

            listing.IsApproved = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Ogłoszenie ID: {CarListingId} zostało zatwierdzone przez admina.", id);

            await _notificationService.CreateNotificationAsync(
                listing.UserId,
                null,
                $"Twoje ogłoszenie zostało zatwierdzone przez administratora"
            );

            return Ok(new { message = "Ogłoszenie zostało zatwierdzone.", listing });
        }

        [HttpPut("{id}/availability")]
        public async Task<IActionResult> UpdateCarAvailability(int id, [FromBody] bool isAvailable)
        {
            _logger.LogInformation("Użytkownik ID: {UserId} próbuje zmienić dostępność ogłoszenia ID: {CarListingId} na {IsAvailable}", GetCurrentUserId(), id, isAvailable);

            var listing = await _context.CarListing.FindAsync(id);

            if (listing == null)
            {
                _logger.LogWarning("Użytkownik ID: {UserId} próbował zmienić dostępność nieistniejącego ogłoszenia ID: {CarListingId}", GetCurrentUserId(), id);
                return NotFound(new { message = ErrorMessages.ListingNotFound });
            }

            if (listing.UserId != GetCurrentUserId() && !IsCurrentUserAdmin())
            {
                _logger.LogWarning("Użytkownik ID: {UserId} próbował zmienić dostępność ogłoszenia ID: {CarListingId}, do którego nie ma uprawnień.", GetCurrentUserId(), id);
                return Unauthorized(new { message = ErrorMessages.NotOwnerOrAdmin });
            }

            listing.IsAvailable = isAvailable;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Dostępność ogłoszenia ID: {CarListingId} zmieniono na {IsAvailable} przez użytkownika ID: {UserId}.", id, isAvailable, GetCurrentUserId());

            return Ok(new { message = "Status dostępności został zmieniony.", listing });
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserCarListings()
        {
            _logger.LogInformation("Użytkownik ID: {UserId} próbuje pobrać swoje ogłoszenia.", GetCurrentUserId());

            var listings = await _context.CarListing
                .Where(l => l.UserId == GetCurrentUserId() && l.IsApproved)
                .ToListAsync();

            if (listings == null || listings.Count == 0)
            {
                _logger.LogInformation("Brak zatwierdzonych ogłoszeń dla użytkownika ID: {UserId}.", GetCurrentUserId());
                return NotFound(new { message = ErrorMessages.NoApprovedListingsForUser });
            }

            _logger.LogInformation("Pomyślnie pobrano {Count} zatwierdzonych ogłoszeń dla użytkownika ID: {UserId}.", listings.Count, GetCurrentUserId());
            return Ok(listings);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllCarListings(double? lat, double? lng, double radius = 50)
        {
            _logger.LogInformation("Pobieranie ogłoszeń dla wszystkich użytkowników. Parametry lokalizacji: Lat={Lat}, Lng={Lng}, Radius={Radius}", lat, lng, radius);

            var query = _context.CarListing
                .Where(l => l.IsApproved && l.UserId != GetCurrentUserId() &&
                            !_context.CarRentals.Any(r => r.CarListingId == l.Id && r.RentalStatus == "Active"));

            var listings = await query.ToListAsync();

            if (lat.HasValue && lng.HasValue)
            {
                var filteredListings = listings.Where(listing =>
                        CalculateDistance(lat.Value, lng.Value, listing.Latitude, listing.Longitude) <= radius)
                    .ToList();

                if (filteredListings.Count == 0)
                {
                    _logger.LogInformation("Brak dostępnych samochodów w podanym regionie dla Lat={Lat}, Lng={Lng}, Radius={Radius}", lat, lng, radius);
                    return Ok(new List<CarListing>());
                }
                _logger.LogInformation("Pomyślnie pobrano {Count} ogłoszeń w promieniu dla Lat={Lat}, Lng={Lng}, Radius={Radius}", filteredListings.Count, lat, lng, radius);
                return Ok(filteredListings);
            }

            if (listings == null || listings.Count == 0)
            {
                _logger.LogInformation("Brak ogłoszeń spełniających kryteria.");
                return Ok(new List<CarListing>());
            }

            _logger.LogInformation("Pomyślnie pobrano {Count} ogłoszeń (bez filtrowania lokalizacji).", listings.Count);
            return Ok(listings);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCarListing(int id)
        {
            _logger.LogInformation("Użytkownik ID: {UserId} próbuje pobrać ogłoszenie ID: {CarListingId}", GetCurrentUserId(), id);
            var listing = await _context.CarListing.FindAsync(id);
            if (listing == null)
            {
                _logger.LogWarning("Próba pobrania nieistniejącego ogłoszenia ID: {CarListingId}", id);
                return NotFound(new { message = ErrorMessages.ListingNotFound });
            }

            _logger.LogInformation("Pomyślnie pobrano ogłoszenie ID: {CarListingId}.", id);
            return Ok(listing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarListing(int id)
        {
            _logger.LogInformation("Użytkownik ID: {UserId} próbuje usunąć ogłoszenie ID: {CarListingId}", GetCurrentUserId(), id);

            var listing = await _context.CarListing.FindAsync(id);

            if (listing == null)
            {
                _logger.LogWarning("Użytkownik ID: {UserId} próbował usunąć nieistniejące ogłoszenie ID: {CarListingId}", GetCurrentUserId(), id);
                return NotFound(new { message = ErrorMessages.ListingNotFound });
            }

            if (listing.UserId != GetCurrentUserId() && !IsCurrentUserAdmin())
            {
                _logger.LogWarning("Użytkownik ID: {UserId} próbował usunąć ogłoszenie ID: {CarListingId}, do którego nie ma uprawnień.", GetCurrentUserId(), id);
                return Unauthorized(new { message = ErrorMessages.NotOwnerOrAdmin });
            }

            var isRented = await _context.CarRentals
                .AnyAsync(r => r.CarListingId == id && r.RentalEndDate >= DateTime.UtcNow);

            if (isRented && !IsCurrentUserAdmin())
            {
                _logger.LogWarning("Użytkownik ID: {UserId} próbował usunąć wypożyczone ogłoszenie ID: {CarListingId}.", GetCurrentUserId(), id);
                return BadRequest(new { message = ErrorMessages.CannotDeleteRentedCar });
            }

            _context.CarListing.Remove(listing);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Ogłoszenie ID: {CarListingId} zostało usunięte przez użytkownika ID: {UserId}.", id, GetCurrentUserId());

            await _notificationService.CreateNotificationAsync(
                listing.UserId,
                null,
                "Twoje ogłoszenie zostało usunięte."
            );

            return Ok("Ogłoszenie usunięte.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCarListing(int id, [FromBody] CarListing updatedListing)
        {
            _logger.LogInformation("Użytkownik ID: {UserId} próbuje zaktualizować ogłoszenie ID: {CarListingId}", GetCurrentUserId(), id);

            var listing = await _context.CarListing.FindAsync(id);

            if (listing == null)
            {
                _logger.LogWarning("Użytkownik ID: {UserId} próbował zaktualizować nieistniejące ogłoszenie ID: {CarListingId}", GetCurrentUserId(), id);
                return NotFound(new { message = ErrorMessages.ListingNotFound });
            }

            if (listing.UserId != GetCurrentUserId() && !IsCurrentUserAdmin())
            {
                _logger.LogWarning("Użytkownik ID: {UserId} próbował zaktualizować ogłoszenie ID: {CarListingId}, do którego nie ma uprawnień.", GetCurrentUserId(), id);
                return Unauthorized(new { message = ErrorMessages.NotOwnerOrAdmin });
            }

            var validationMessage = ValidateCarListing(updatedListing);
            if (validationMessage != null)
            {
                _logger.LogWarning("Niepoprawne dane ogłoszenia podczas aktualizacji ogłoszenia ID: {CarListingId}: {Message}", id, validationMessage);
                return BadRequest(new { message = validationMessage });
            }

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
            _logger.LogInformation("Ogłoszenie ID: {CarListingId} zostało zaktualizowane przez użytkownika ID: {UserId}.", id, GetCurrentUserId());

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

        private async Task NotifyAdminsAboutNewListing(int userId)
        {
            var admins = await _context.Users
                .Where(u => u.Role == Roles.Admin)
                .ToListAsync();

            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(
                    admin.Id,
                    null,
                    $"Nowe ogłoszenie oczekuję na zatwierdzenie"
                );
            }
            _logger.LogInformation("Powiadomiono administratorów o nowym ogłoszeniu od użytkownika ID: {UserId}.", userId);
        }
    }
}