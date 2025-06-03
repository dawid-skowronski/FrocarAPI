using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FrogCar.Data;
using FrogCar.Models;
using System.Linq;
using System.Threading.Tasks;
using FrogCar.Constants;

namespace FrogCar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Admin)] 
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordValidator _passwordValidator; 
        private readonly ILogger<AdminController> _logger; 

        public AdminController(
            AppDbContext context,
            IPasswordValidator passwordValidator, 
            ILogger<AdminController> logger) 
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _passwordValidator = passwordValidator ?? throw new ArgumentNullException(nameof(passwordValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            _logger.LogInformation("Admin próbuje pobrać listę użytkowników.");
            var users = await _context.Users
                .Select(u => new { u.Id, u.Username, u.Email, u.Role })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            _logger.LogInformation("Admin próbuje pobrać użytkownika o ID: {UserId}", id);
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                _logger.LogWarning("Admin próbował pobrać nieistniejącego użytkownika o ID: {UserId}", id);
                return NotFound(new { message = Constants.ErrorMessages.UserNotFound });
            }

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
            _logger.LogInformation("Admin próbuje pobrać listę ogłoszeń.");
            var listings = await _context.CarListing.Include(c => c.User).ToListAsync();
            return Ok(listings);
        }

        [HttpGet("rentals")]
        public async Task<IActionResult> GetAllRentals()
        {
            _logger.LogInformation("Admin próbuje pobrać listę wypożyczeń.");
            var rentals = await _context.CarRentals
                .Include(r => r.User)
                .Include(r => r.CarListing)
                .ToListAsync();

            return Ok(rentals);
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> GetAllReviews()
        {
            _logger.LogInformation("Admin próbuje pobrać listę recenzji.");
            var reviews = await _context.CarRentalReviews
                .Include(r => r.User)
                .Include(r => r.CarRental)
                    .ThenInclude(cr => cr.CarListing)
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            _logger.LogInformation("Admin próbuje usunąć użytkownika o ID: {UserId}", id);
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Admin próbował usunąć nieistniejącego użytkownika o ID: {UserId}", id);
                return NotFound(new { message = Constants.ErrorMessages.UserNotFound });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Użytkownik o ID: {UserId} został usunięty przez admina.", id);

            return Ok("Użytkownik został usunięty.");
        }

        [HttpDelete("review/{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            _logger.LogInformation("Admin próbuje usunąć recenzję o ID: {ReviewId}", reviewId);
            var review = await _context.CarRentalReviews
                .Include(r => r.CarRental)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
            {
                _logger.LogWarning("Admin próbował usunąć nieistniejącą recenzję o ID: {ReviewId}", reviewId);
                return NotFound(new { message = "Recenzja nie istnieje." });
            }

            var associatedCarListingId = review.CarRental.CarListingId;

            _context.CarRentalReviews.Remove(review);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Recenzja o ID: {ReviewId} została usunięta przez admina.", reviewId);

            await UpdateListingAverageRating(associatedCarListingId);

            return Ok("Recenzja została usunięta.");
        }

        [HttpPut("user/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserModel model)
        {
            _logger.LogInformation("Admin próbuje zaktualizować użytkownika o ID: {UserId}", id);
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Admin próbował zaktualizować nieistniejącego użytkownika o ID: {UserId}", id);
                return NotFound(new { message = Constants.ErrorMessages.UserNotFound });
            }

            if (!string.IsNullOrWhiteSpace(model.Username))
            {
                if (model.Username != user.Username && await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    _logger.LogWarning("Admin próbował zmienić nazwę użytkownika na już istniejącą: {Username}", model.Username);
                    return BadRequest(new { message = Constants.ErrorMessages.UsernameTaken });
                }
                user.Username = model.Username;
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                if (model.Email != user.Email && await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    _logger.LogWarning("Admin próbował zmienić email użytkownika na już istniejący: {Email}", model.Email);
                    return BadRequest(new { message = Constants.ErrorMessages.EmailTaken });
                }
                user.Email = model.Email;
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var passwordValidationMessage = _passwordValidator.Validate(model.Password);
                if (passwordValidationMessage != null)
                {
                    _logger.LogWarning("Błąd walidacji hasła podczas aktualizacji użytkownika {UserId}: {Error}", id, passwordValidationMessage);
                    return BadRequest(new { message = passwordValidationMessage });
                }
                user.Password = _passwordValidator.Hash(model.Password);
            }

            if (!string.IsNullOrWhiteSpace(model.Role))
            {
                if (model.Role != Constants.Roles.User && model.Role != Constants.Roles.Admin)
                {
                    _logger.LogWarning("Admin próbował ustawić niepoprawną rolę dla użytkownika {UserId}: {Role}", id, model.Role);
                    return BadRequest(new { message = "Niepoprawna rola. Dozwolone role: User, Admin." });
                }
                user.Role = model.Role;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Użytkownik o ID: {UserId} został zaktualizowany przez admina.", id);

            return Ok("Użytkownik został zaktualizowany.");
        }

        [HttpGet("admin/stats")]
        public async Task<IActionResult> GetAdminStats()
        {
            _logger.LogInformation("Admin próbuje pobrać statystyki ogólne.");
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalListings = await _context.CarListing.CountAsync(),
                ActiveListings = await _context.CarListing.CountAsync(c => c.IsAvailable && c.IsApproved),
                TotalRentals = await _context.CarRentals.CountAsync(),
                EndedRentals = await _context.CarRentals.CountAsync(r => r.RentalStatus == "Zakończone"),
                TotalReviews = await _context.CarRentalReviews.CountAsync(),
                AverageRating = Math.Round(await _context.CarRentalReviews.AverageAsync(r => (double?)r.Rating) ?? 0, 2)
            };
            _logger.LogInformation("Admin pobrał statystyki ogólne.");
            return Ok(stats);
        }

        [HttpGet("admin/finance-stats")]
        public async Task<IActionResult> GetFinanceStats()
        {
            _logger.LogInformation("Admin próbuje pobrać statystyki finansowe.");
            var totalRevenue = await _context.CarRentals.SumAsync(r => (decimal?)r.RentalPrice) ?? 0;
            var endedRentals = await _context.CarRentals.CountAsync(r => r.RentalStatus == "Zakończone");
            var averageRevenue = endedRentals > 0 ? totalRevenue / endedRentals : 0;

            var last30DaysRevenue = await _context.CarRentals
                .Where(r => r.RentalStartDate >= DateTime.UtcNow.AddDays(-30))
                .SumAsync(r => (decimal?)r.RentalPrice) ?? 0;

            var financeStats = new
            {
                TotalRevenue = Math.Round(totalRevenue, 2),
                AverageRevenue = Math.Round(averageRevenue, 2),
                Last30DaysRevenue = Math.Round(last30DaysRevenue, 2)
            };
            _logger.LogInformation("Admin pobrał statystyki finansowe.");
            return Ok(financeStats);
        }

        [HttpGet("admin/top-rented-cars")]
        public async Task<IActionResult> GetTopRentedCars()
        {
            _logger.LogInformation("Admin próbuje pobrać najczęściej wypożyczane samochody.");
            var topCars = await _context.CarRentals
                .GroupBy(r => r.CarListingId)
                .Select(g => new
                {
                    CarListingId = g.Key,
                    RentalCount = g.Count(),
                    Listing = g.First().CarListing 
                })
                .OrderByDescending(x => x.RentalCount)
                .Take(3)
                .ToListAsync();

            _logger.LogInformation("Admin pobrał najczęściej wypożyczane samochody.");
            return Ok(topCars);
        }

        [HttpGet("admin/top-rated-cars")]
        public async Task<IActionResult> GetTopRatedCars()
        {
            _logger.LogInformation("Admin próbuje pobrać najwyżej oceniane samochody.");
            var topRated = await _context.CarRentalReviews
                .Include(r => r.CarRental) 
                .ThenInclude(cr => cr.CarListing) 
                .GroupBy(r => r.CarRental.CarListingId)
                .Where(g => g.Count() >= 3)
                .Select(g => new
                {
                    CarListingId = g.Key,
                    AverageRating = g.Average(r => r.Rating),
                    ReviewCount = g.Count(),
                    Listing = g.First().CarRental.CarListing 
                })
                .OrderByDescending(x => x.AverageRating)
                .Take(3)
                .ToListAsync();

            _logger.LogInformation("Admin pobrał najwyżej oceniane samochody.");
            return Ok(topRated);
        }

        [HttpGet("admin/top-users")]
        public async Task<IActionResult> GetTopUsers()
        {
            _logger.LogInformation("Admin próbuje pobrać statystyki użytkowników (najlepsi właściciele/najemcy).");
            var topOwners = await _context.CarListing
                .Include(c => c.User) 
                .GroupBy(c => c.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    ListingsCount = g.Count(),
                    User = new { g.First().User.Id, g.First().User.Username, g.First().User.Email } 
                })
                .OrderByDescending(x => x.ListingsCount)
                .Take(3)
                .ToListAsync();

            var topRenters = await _context.CarRentals
                .Include(r => r.User) 
                .GroupBy(r => r.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    RentalsCount = g.Count(),
                    User = new { g.First().User.Id, g.First().User.Username, g.First().User.Email } 
                })
                .OrderByDescending(x => x.RentalsCount)
                .Take(3)
                .ToListAsync();

            _logger.LogInformation("Admin pobrał statystyki użytkowników.");
            return Ok(topRenters);
        }
        private async Task UpdateListingAverageRating(int carListingId)
        {
            var averageRating = await _context.CarRentalReviews
                .Where(r => r.CarRental.CarListingId == carListingId)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            var listing = await _context.CarListing.FindAsync(carListingId);
            if (listing != null)
            {
                listing.AverageRating = Math.Round(averageRating, 2);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Zaktualizowano średnią ocenę dla ogłoszenia ID: {CarListingId} na {AverageRating}", carListingId, listing.AverageRating);
            }
        }
    }
}