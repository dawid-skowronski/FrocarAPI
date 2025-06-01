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

    [HttpGet("rentals")]
    public async Task<IActionResult> GetAllRentals()
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

        var rentals = await _context.CarRentals
            .Include(r => r.User)
            .Include(r => r.CarListing)
            .ToListAsync();

        return Ok(rentals);
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> GetAllReviews()
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

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
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("Użytkownik nie istnieje.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok("Użytkownik został usunięty.");
    }

    [HttpDelete("review/{reviewId}")]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

        var review = await _context.CarRentalReviews
            .Include(r => r.CarRental)
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

        if (review == null)
            return NotFound("Recenzja nie istnieje.");

        var carListingId = review.CarRental.CarListingId;

        _context.CarRentalReviews.Remove(review);
        await _context.SaveChangesAsync();

        var averageRating = await _context.CarRentalReviews
            .Where(r => r.CarRental.CarListingId == carListingId)
            .AverageAsync(r => (double?)r.Rating) ?? 0;

        var listing = await _context.CarListing.FindAsync(carListingId);
        if (listing != null)
        {
            listing.AverageRating = Math.Round(averageRating, 2);
            await _context.SaveChangesAsync();
        }

        return Ok("Recenzja została usunięta.");
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
        {
            var passwordValidationMessage = ValidatePassword(model.Password);
            if (passwordValidationMessage != null)
                return BadRequest(passwordValidationMessage);

            user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
        }

        await _context.SaveChangesAsync();

        return Ok("Użytkownik został zaktualizowany.");
    }

    [HttpGet("admin/stats")]
    public async Task<IActionResult> GetAdminStats()
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

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

        return Ok(stats);
    }

    [HttpGet("admin/finance-stats")]
    public async Task<IActionResult> GetFinanceStats()
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

        var totalRevenue = await _context.CarRentals.SumAsync(r => (decimal?)r.RentalPrice) ?? 0;
        var endedRentals = await _context.CarRentals.CountAsync(r => r.RentalStatus == "Zakończone");
        var averageRevenue = endedRentals > 0 ? totalRevenue / endedRentals : 0;

        var last30DaysRevenue = await _context.CarRentals
            .Where(r => r.RentalStartDate >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(r => (decimal?)r.RentalPrice) ?? 0;

        return Ok(new
        {
            TotalRevenue = Math.Round(totalRevenue, 2),
            AverageRevenue = Math.Round(averageRevenue, 2),
            Last30DaysRevenue = Math.Round(last30DaysRevenue, 2)
        });
    }

    [HttpGet("admin/top-rented-cars")]
    public async Task<IActionResult> GetTopRentedCars()
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

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

        return Ok(topCars);
    }

    [HttpGet("admin/top-rated-cars")]
    public async Task<IActionResult> GetTopRatedCars()
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

        var topRated = await _context.CarRentalReviews
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

        return Ok(topRated);
    }
    [HttpGet("admin/top-users")]
    public async Task<IActionResult> GetTopUsers()
    {
        if (!IsAdmin())
            return BadRequest("Brak uprawnień administratora.");

        var topOwners = await _context.CarListing
            .GroupBy(c => c.UserId)
            .Select(g => new {
                UserId = g.Key,
                ListingsCount = g.Count(),
                User = g.First().User
            })
            .OrderByDescending(x => x.ListingsCount)
            .Take(3)
            .ToListAsync();

        var topRenters = await _context.CarRentals
            .GroupBy(r => r.UserId)
            .Select(g => new {
                UserId = g.Key,
                RentalsCount = g.Count(),
                User = g.First().User
            })
            .OrderByDescending(x => x.RentalsCount)
            .Take(3)
            .ToListAsync();

        return Ok(new { TopOwners = topOwners, TopRenters = topRenters });
    }


    private string ValidatePassword(string password)
    {
        if (password.Length < 8)
            return "Hasło musi mieć co najmniej 8 znaków.";

        if (!password.Any(char.IsUpper))
            return "Hasło musi zawierać co najmniej jedną wielką literę.";

        if (!password.Any(char.IsDigit))
            return "Hasło musi zawierać co najmniej jedną cyfrę.";

        var specialCharacters = "!@#$%^&*()-_=+[]{}|;:'\",.<>?/";
        if (!password.Any(c => specialCharacters.Contains(c)))
            return "Hasło musi zawierać co najmniej jeden znak specjalny.";

        return null;
    }


    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

}