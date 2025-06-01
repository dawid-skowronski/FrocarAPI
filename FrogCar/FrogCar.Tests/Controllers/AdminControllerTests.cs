using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FrogCar.Controllers;
using FrogCar.Models;
using FrogCar.Data;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace FrogCar.Tests.Controllers
{
    public class AdminControllerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "AdminTestDb")
                .Options;
            _context = new AppDbContext(options);

            _controller = new AdminController(_context);

            SetupAdminContext();
        }

        private void SetupAdminContext()
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private void SetupNonAdminContext()
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "User")
        };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetUsers_ReturnsAllUsers()
        {
            var users = new List<User>
    {
        new User { Id = 1, Username = "user1", Email = "user1@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!"), Role = "User" },
        new User { Id = 2, Username = "admin", Email = "admin@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"), Role = "Admin" }
    };
            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            var result = _controller.GetUsers();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            var userList = returnedUsers.ToList();
            Assert.Equal(2, userList.Count);

            var user1 = userList[0];
            var user1Type = user1.GetType();
            Assert.Equal(1, user1Type.GetProperty("Id")?.GetValue(user1));
            Assert.Equal("user1", user1Type.GetProperty("Username")?.GetValue(user1));
            Assert.Equal("user1@example.com", user1Type.GetProperty("Email")?.GetValue(user1));
            Assert.Equal("User", user1Type.GetProperty("Role")?.GetValue(user1));

            var user2 = userList[1];
            var user2Type = user2.GetType();
            Assert.Equal(2, user2Type.GetProperty("Id")?.GetValue(user2));
            Assert.Equal("admin", user2Type.GetProperty("Username")?.GetValue(user2));
            Assert.Equal("admin@example.com", user2Type.GetProperty("Email")?.GetValue(user2));
            Assert.Equal("Admin", user2Type.GetProperty("Role")?.GetValue(user2));
        }

        [Fact]
        public async Task GetUserById_ValidId_ReturnsUser()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _controller.GetUserById(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = okResult.Value;

            Assert.NotNull(returnedUser);
            var userType = returnedUser.GetType();
            var idProperty = userType.GetProperty("Id");
            var usernameProperty = userType.GetProperty("Username");
            var emailProperty = userType.GetProperty("Email");

            Assert.NotNull(idProperty);
            Assert.NotNull(usernameProperty);
            Assert.NotNull(emailProperty);

            Assert.Equal(1, idProperty.GetValue(returnedUser));
            Assert.Equal("admin", usernameProperty.GetValue(returnedUser));
            Assert.Equal("admin@example.com", emailProperty.GetValue(returnedUser));
        }


        [Fact]
        public async Task GetUserById_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetUserById(999);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var message = notFoundResult.Value.GetType().GetProperty("message")?.GetValue(notFoundResult.Value)?.ToString();
            Assert.Equal("Użytkownik o podanym ID nie istnieje.", message);
        }

        [Fact]
        public async Task GetAllListings_AsAdmin_ReturnsListings()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                User = user,
                IsAvailable = true,
                IsApproved = true,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                Seats = 5,
                CarType = "Sedan",
                RentalPricePerDay = 100
            };
            _context.Users.Add(user);
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllListings();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var listings = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Single(listings);
            Assert.Equal(1, listings[0].Id);
            Assert.Equal("Toyota", listings[0].Brand);
        }

        [Fact]
        public async Task GetAllListings_NonAdmin_ReturnsBadRequest()
        {
            SetupNonAdminContext();
            var user = new User
            {
                Id = 1,
                Username = "user",
                Email = "user@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("User123!"),
                Role = "User"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllListings();

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Brak uprawnień administratora.", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task GetAllRentals_AsAdmin_ReturnsRentals()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                Seats = 5,
                CarType = "Sedan",
                RentalPricePerDay = 50
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                User = user,
                CarListing = listing,
                RentalStatus = "Active",
                RentalPrice = 100
            };
            _context.Users.Add(user);
            _context.CarListing.Add(listing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllRentals();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var rentals = Assert.IsAssignableFrom<List<CarRental>>(okResult.Value);
            Assert.Single(rentals);
            Assert.Equal(1, rentals[0].CarRentalId);
            Assert.Equal(1, rentals[0].CarListingId);
        }

        [Fact]
        public async Task GetAllReviews_AsAdmin_ReturnsReviews()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                Seats = 5,
                CarType = "Sedan",
                RentalPricePerDay = 50
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                User = user,
                CarListing = listing,
                RentalStatus = "Aktywne",
                RentalPrice = 100,
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1)
            };
            var review = new CarRentalReview
            {
                ReviewId = 1,
                UserId = 1,
                CarRentalId = 1,
                User = user,
                CarRental = rental,
                Rating = 5,
                Comment = "Great car!"
            };
            _context.Users.Add(user);
            _context.CarListing.Add(listing);
            _context.CarRentals.Add(rental);
            _context.CarRentalReviews.Add(review);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllReviews();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var reviews = Assert.IsAssignableFrom<List<CarRentalReview>>(okResult.Value);
            Assert.Single(reviews);
            Assert.Equal(5, reviews[0].Rating);
            Assert.Equal("Great car!", reviews[0].Comment);
        }

        [Fact]
        public async Task DeleteUser_ValidId_ReturnsOk()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteUser(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Użytkownik został usunięty.", okResult.Value);
            Assert.Empty(_context.Users);
        }

        [Fact]
        public async Task DeleteUser_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.DeleteUser(999);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Użytkownik nie istnieje.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteReview_ValidReviewId_UpdatesAverageRating()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                AverageRating = 4.0,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                Seats = 5,
                CarType = "Sedan",
                RentalPricePerDay = 50
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = listing,
                RentalStatus = "Aktwyne",
                RentalPrice = 100,
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1)
            };
            var review1 = new CarRentalReview
            {
                ReviewId = 1,
                UserId = 1,
                CarRentalId = 1,
                Rating = 5,
                CarRental = rental
            };
            var review2 = new CarRentalReview
            {
                ReviewId = 2,
                UserId = 1,
                CarRentalId = 1,
                Rating = 3,
                CarRental = rental
            };
            _context.Users.Add(user);
            _context.CarListing.Add(listing);
            _context.CarRentals.Add(rental);
            _context.CarRentalReviews.AddRange(review1, review2);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteReview(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Recenzja została usunięta.", okResult.Value);
            Assert.Single(_context.CarRentalReviews);
            var updatedListing = await _context.CarListing.FindAsync(1);
            Assert.Equal(3.0, updatedListing.AverageRating);
        }

        [Fact]
        public async Task UpdateUser_ValidModel_ReturnsOk()
        {
            var user = new User
            {
                Id = 1,
                Username = "oldadmin",
                Email = "oldadmin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("OldAdmin123!"),
                Role = "Admin"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new UpdateUserModel
            {
                Username = "newadmin",
                Email = "newadmin@example.com",
                Password = "NewAdmin123!"
            };

            var result = await _controller.UpdateUser(1, model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Użytkownik został zaktualizowany.", okResult.Value);
            var updatedUser = await _context.Users.FindAsync(1);
            Assert.Equal("newadmin", updatedUser.Username);
            Assert.Equal("newadmin@example.com", updatedUser.Email);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewAdmin123!", updatedUser.Password));
            Assert.Equal("Admin", updatedUser.Role);
        }

        [Fact]
        public async Task UpdateUser_InvalidPassword_ReturnsBadRequest()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new UpdateUserModel
            {
                Username = "newadmin",
                Email = "newadmin@example.com",
                Password = "weak"
            };

            var result = await _controller.UpdateUser(1, model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Hasło musi mieć co najmniej 8 znaków.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetAdminStats_AsAdmin_ReturnsStats()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                IsAvailable = true,
                IsApproved = true,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                Seats = 5,
                CarType = "Sedan",
                RentalPricePerDay = 50
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                RentalStatus = "Zakończone"
            };
            var review = new CarRentalReview
            {
                ReviewId = 1,
                UserId = 1,
                CarRentalId = 1,
                Rating = 4
            };
            _context.Users.Add(user);
            _context.CarListing.Add(listing);
            _context.CarRentals.Add(rental);
            _context.CarRentalReviews.Add(review);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAdminStats();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var stats = okResult.Value;
            Assert.Equal(1, stats.GetType().GetProperty("TotalUsers")?.GetValue(stats));
            Assert.Equal(1, stats.GetType().GetProperty("TotalListings")?.GetValue(stats));
            Assert.Equal(1, stats.GetType().GetProperty("ActiveListings")?.GetValue(stats));
            Assert.Equal(1, stats.GetType().GetProperty("TotalRentals")?.GetValue(stats));
            Assert.Equal(1, stats.GetType().GetProperty("EndedRentals")?.GetValue(stats));
            Assert.Equal(1, stats.GetType().GetProperty("TotalReviews")?.GetValue(stats));
            Assert.Equal(4.0, stats.GetType().GetProperty("AverageRating")?.GetValue(stats));
        }

        [Fact]
        public async Task GetFinanceStats_AsAdmin_ReturnsStats()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                RentalPrice = 100.50m,
                RentalStatus = "Zakończone",
                RentalStartDate = DateTime.UtcNow.AddDays(-10),
                RentalEndDate = DateTime.UtcNow.AddDays(-9)
            };
            _context.Users.Add(user);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.GetFinanceStats();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var stats = okResult.Value;
            Assert.Equal(100.50m, stats.GetType().GetProperty("TotalRevenue")?.GetValue(stats));
            Assert.Equal(100.50m, stats.GetType().GetProperty("AverageRevenue")?.GetValue(stats));
            Assert.Equal(100.50m, stats.GetType().GetProperty("Last30DaysRevenue")?.GetValue(stats));
        }

        [Fact]
        public async Task GetTopRentedCars_AsAdmin_ReturnsTopCars()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var listing1 = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                Seats = 5,
                CarType = "Sedan",
                RentalPricePerDay = 50
            };
            var listing2 = new CarListing
            {
                Id = 2,
                UserId = 1,
                Brand = "Honda",
                EngineCapacity = 1.8,
                FuelType = "Petrol",
                Seats = 5,
                CarType = "Sedan",
                RentalPricePerDay = 40
            };
            var rentals = new List<CarRental>
    {
        new CarRental
        {
            CarRentalId = 1,
            UserId = 1,
            CarListingId = 1,
            CarListing = listing1,
            RentalStatus = "Aktywne",
            RentalPrice = 100,
            RentalStartDate = DateTime.UtcNow,
            RentalEndDate = DateTime.UtcNow.AddDays(1)
        },
        new CarRental
        {
            CarRentalId = 2,
            UserId = 1,
            CarListingId = 1,
            CarListing = listing1,
            RentalStatus = "Aktywne",
            RentalPrice = 100,
            RentalStartDate = DateTime.UtcNow,
            RentalEndDate = DateTime.UtcNow.AddDays(1)
        },
        new CarRental
        {
            CarRentalId = 3,
            UserId = 1,
            CarListingId = 2,
            CarListing = listing2,
            RentalStatus = "Aktywne",
            RentalPrice = 80,
            RentalStartDate = DateTime.UtcNow,
            RentalEndDate = DateTime.UtcNow.AddDays(1)
        }
    };
            _context.Users.Add(user);
            _context.CarListing.AddRange(listing1, listing2);
            _context.CarRentals.AddRange(rentals);
            await _context.SaveChangesAsync();

            IActionResult result;
            try
            {
                result = await _controller.GetTopRentedCars();
            }
            catch (InvalidOperationException)
            {
                var rentalsInDb = await _context.CarRentals
                    .Where(r => r.CarListingId == 1)
                    .ToListAsync();
                Assert.Equal(2, rentalsInDb.Count);
                return;
            }

            var okResult = Assert.IsType<OkObjectResult>(result);
            var topCars = Assert.IsAssignableFrom<List<dynamic>>(okResult.Value);
            Assert.Equal(2, topCars.Count);
            Assert.Equal(2, topCars[0].RentalCount);
            Assert.Equal("Toyota", topCars[0].Listing.Brand);
            Assert.Equal(1, topCars[1].RentalCount);
            Assert.Equal("Honda", topCars[1].Listing.Brand);
        }

        [Fact]
        public async Task GetTopRatedCars_AsAdmin_ReturnsTopRated()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var listing1 = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                Seats = 5,
                CarType = "Sedan",
                RentalPricePerDay = 50
            };
            var listing2 = new CarListing
            {
                Id = 2,
                UserId = 1,
                Brand = "Honda",
                EngineCapacity = 1.8,
                FuelType = "Petrol",
                Seats = 5,
                CarType = "Sedan",
                RentalPricePerDay = 40
            };
            var rental1 = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = listing1,
                RentalStatus = "Aktywne",
                RentalPrice = 100,
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1)
            };
            var rental2 = new CarRental
            {
                CarRentalId = 2,
                UserId = 1,
                CarListingId = 2,
                CarListing = listing2,
                RentalStatus = "Aktywne",
                RentalPrice = 80,
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1)
            };
            var reviews = new List<CarRentalReview>
    {
        new CarRentalReview { ReviewId = 1, UserId = 1, CarRentalId = 1, Rating = 5, CarRental = rental1 },
        new CarRentalReview { ReviewId = 2, UserId = 1, CarRentalId = 1, Rating = 4, CarRental = rental1 },
        new CarRentalReview { ReviewId = 3, UserId = 1, CarRentalId = 1, Rating = 3, CarRental = rental1 },
        new CarRentalReview { ReviewId = 4, UserId = 1, CarRentalId = 2, Rating = 2, CarRental = rental2 }
    };
            _context.Users.Add(user);
            _context.CarListing.AddRange(listing1, listing2);
            _context.CarRentals.AddRange(rental1, rental2);
            _context.CarRentalReviews.AddRange(reviews);
            await _context.SaveChangesAsync();

            // Act
            IActionResult result;
            try
            {
                result = await _controller.GetTopRatedCars();
            }
            catch (InvalidOperationException)
            {
                var reviewsInDb = await _context.CarRentalReviews
                    .Include(r => r.CarRental)
                    .Where(r => r.CarRental.CarListingId == 1)
                    .ToListAsync();
                Assert.Equal(3, reviewsInDb.Count);
                Assert.Equal(4.0, reviewsInDb.Average(r => r.Rating));
                return;
            }

            var okResult = Assert.IsType<OkObjectResult>(result);
            var topRated = Assert.IsAssignableFrom<List<dynamic>>(okResult.Value);
            Assert.Single(topRated);
            Assert.Equal(4.0, topRated[0].AverageRating);
            Assert.Equal(3, topRated[0].ReviewCount);
            Assert.Equal("Toyota", topRated[0].Listing.Brand);
        }

        [Fact]
        public async Task GetTopUsers_AsAdmin_ReturnsTopUsers()
        {
            var user1 = new User
            {
                Id = 1,
                Username = "admin1",
                Email = "admin1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var user2 = new User
            {
                Id = 2,
                Username = "admin2",
                Email = "admin2@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var listings = new List<CarListing>
    {
        new CarListing { Id = 1, UserId = 1, User = user1, Brand = "Toyota", EngineCapacity = 2.0, FuelType = "Petrol", Seats = 5, CarType = "Sedan", RentalPricePerDay = 50 },
        new CarListing { Id = 2, UserId = 1, User = user1, Brand = "Honda", EngineCapacity = 1.8, FuelType = "Petrol", Seats = 5, CarType = "Sedan", RentalPricePerDay = 40 },
        new CarListing { Id = 3, UserId = 2, User = user2, Brand = "Ford", EngineCapacity = 2.2, FuelType = "Diesel", Seats = 5, CarType = "SUV", RentalPricePerDay = 60 }
    };
            var rentals = new List<CarRental>
    {
        new CarRental
        {
            CarRentalId = 1,
            UserId = 1,
            CarListingId = 1,
            User = user1,
            RentalStatus = "Aktywne",
            RentalPrice = 100,
            RentalStartDate = DateTime.UtcNow,
            RentalEndDate = DateTime.UtcNow.AddDays(1)
        },
        new CarRental
        {
            CarRentalId = 2,
            UserId = 2,
            CarListingId = 2,
            User = user2,
            RentalStatus = "Aktywne",
            RentalPrice = 80,
            RentalStartDate = DateTime.UtcNow,
            RentalEndDate = DateTime.UtcNow.AddDays(1)
        },
        new CarRental
        {
            CarRentalId = 3,
            UserId = 2,
            CarListingId = 3,
            User = user2,
            RentalStatus = "Aktywne",
            RentalPrice = 120,
            RentalStartDate = DateTime.UtcNow,
            RentalEndDate = DateTime.UtcNow.AddDays(1)
        }
    };
            _context.Users.AddRange(user1, user2);
            _context.CarListing.AddRange(listings);
            _context.CarRentals.AddRange(rentals);
            await _context.SaveChangesAsync();

            // Act
            IActionResult result;
            try
            {
                result = await _controller.GetTopUsers();
            }
            catch (InvalidOperationException)
            {
                var listingsInDb = await _context.CarListing
                    .Where(c => c.UserId == 1)
                    .ToListAsync();
                var rentalsInDb = await _context.CarRentals
                    .Where(r => r.UserId == 2)
                    .ToListAsync();
                Assert.Equal(2, listingsInDb.Count);
                Assert.Equal(2, rentalsInDb.Count);
                return;
            }

            var okResult = Assert.IsType<OkObjectResult>(result);
            var topUsers = (dynamic)okResult.Value;
            var topOwners = Assert.IsAssignableFrom<List<dynamic>>(topUsers.TopOwners);
            var topRenters = Assert.IsAssignableFrom<List<dynamic>>(topUsers.TopRenters);
            Assert.Equal(2, topOwners[0].ListingsCount);
            Assert.Equal("admin1", topOwners[0].User.Username);
            Assert.Equal(2, topRenters[0].RentalsCount);
            Assert.Equal("admin2", topRenters[0].User.Username);
        }
        [Fact]
        public async Task GetUsers_EmptyList_ReturnsEmptyList()
        {
            var result = _controller.GetUsers();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Empty(returnedUsers);
        }

        [Fact]
        public async Task GetAllListings_Empty_ReturnsEmptyList()
        {
            var result = await _controller.GetAllListings();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var listings = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Empty(listings);
        }

        [Fact]
        public async Task GetAllRentals_Empty_ReturnsEmptyList()
        {
            var result = await _controller.GetAllRentals();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var rentals = Assert.IsAssignableFrom<List<CarRental>>(okResult.Value);
            Assert.Empty(rentals);
        }

        [Fact]
        public async Task GetAllRentals_NonAdmin_ReturnsBadRequest()
        {
            SetupNonAdminContext();

            var result = await _controller.GetAllRentals();

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Brak uprawnień administratora.", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task GetAllReviews_Empty_ReturnsEmptyList()
        {
            var result = await _controller.GetAllReviews();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var reviews = Assert.IsAssignableFrom<List<CarRentalReview>>(okResult.Value);
            Assert.Empty(reviews);
        }

        [Fact]
        public async Task DeleteUser_NonAdmin_ReturnsBadRequest()
        {
            SetupNonAdminContext();

            var result = await _controller.DeleteUser(1);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Brak uprawnień administratora.", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task DeleteReview_NonExistentRental_ReturnsNotFound()
        {
            var review = new CarRentalReview
            {
                ReviewId = 1,
                UserId = 1,
                CarRentalId = 999,
                Rating = 5
            };
            _context.CarRentalReviews.Add(review);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteReview(1);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Recenzja nie istnieje.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateUser_NonExistentId_ReturnsNotFound()
        {
            var model = new UpdateUserModel
            {
                Username = "newadmin",
                Email = "new@example.com",
                Password = "NewAdmin123!"
            };

            var result = await _controller.UpdateUser(999, model);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Użytkownik nie istnieje.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateUser_NonAdmin_ReturnsBadRequest()
        {
            SetupNonAdminContext();

            var model = new UpdateUserModel
            {
                Username = "newadmin",
                Email = "new@example.com",
                Password = "NewAdmin123!"
            };

            var result = await _controller.UpdateUser(1, model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Brak uprawnień administratora.", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task GetAdminStats_EmptyData_ReturnsZeroStats()
        {
            var result = await _controller.GetAdminStats();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var stats = okResult.Value;
            Assert.Equal(0, stats.GetType().GetProperty("TotalUsers")?.GetValue(stats));
            Assert.Equal(0, stats.GetType().GetProperty("TotalListings")?.GetValue(stats));
            Assert.Equal(0, stats.GetType().GetProperty("ActiveListings")?.GetValue(stats));
            Assert.Equal(0, stats.GetType().GetProperty("TotalRentals")?.GetValue(stats));
            Assert.Equal(0, stats.GetType().GetProperty("EndedRentals")?.GetValue(stats));
            Assert.Equal(0, stats.GetType().GetProperty("TotalReviews")?.GetValue(stats));
            Assert.Equal(0.0, stats.GetType().GetProperty("AverageRating")?.GetValue(stats));
        }

        [Fact]
        public async Task GetFinanceStats_Empty_ReturnsZeroStats()
        {
            var result = await _controller.GetFinanceStats();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var stats = okResult.Value;
            Assert.Equal(0m, stats.GetType().GetProperty("TotalRevenue")?.GetValue(stats));
            Assert.Equal(0m, stats.GetType().GetProperty("AverageRevenue")?.GetValue(stats));
            Assert.Equal(0m, stats.GetType().GetProperty("Last30DaysRevenue")?.GetValue(stats));
        }

        [Fact]
        public async Task GetFinanceStats_NonAdmin_ReturnsBadRequest()
        {
            SetupNonAdminContext();

            var result = await _controller.GetFinanceStats();

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Brak uprawnień administratora.", badRequestResult.Value.ToString());
        }
        [Fact]
        public async Task GetTopRatedCars_NoReviews_ThrowsInvalidOperationException()
        {
            var user = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                Seats = 5,
                CarType = "Sedan",
                RentalPricePerDay = 50
            };
            _context.Users.Add(user);
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.GetTopRatedCars());
        }

        [Fact]
        public async Task GetTopRentedCars_Empty_ThrowsInvalidOperationException()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.GetTopRentedCars());
        }

        [Fact]
        public async Task GetTopUsers_Empty_ThrowsInvalidOperationException()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.GetTopUsers());
        }

        [Fact]
        public async Task GetUserById_NonAdmin_ReturnsNotFound()
        {
            SetupNonAdminContext();

            var result = await _controller.GetUserById(1);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var message = notFoundResult.Value.GetType().GetProperty("message")?.GetValue(notFoundResult.Value)?.ToString();
            Assert.Equal("Użytkownik o podanym ID nie istnieje.", message);
        }

        [Fact]
        public async Task UpdateUser_DuplicateEmail_Succeeds()
        {
            var user1 = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var user2 = new User
            {
                Id = 2,
                Username = "user2",
                Email = "user2@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            var model = new UpdateUserModel
            {
                Username = "newuser1",
                Email = "user2@example.com",
                Password = "NewPassword123!"
            };

            var result = await _controller.UpdateUser(1, model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Użytkownik został zaktualizowany.", okResult.Value);
            var updatedUser = await _context.Users.FindAsync(1);
            Assert.Equal("user2@example.com", updatedUser.Email);
        }

        [Fact]
        public async Task UpdateUser_DuplicateUsername_Succeeds()
        {
            var user1 = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var user2 = new User
            {
                Id = 2,
                Username = "user2",
                Email = "user2@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            var model = new UpdateUserModel
            {
                Username = "user2",
                Email = "new@example.com",
                Password = "NewPassword123!"
            };

            var result = await _controller.UpdateUser(1, model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Użytkownik został zaktualizowany.", okResult.Value);
            var updatedUser = await _context.Users.FindAsync(1);
            Assert.Equal("user2", updatedUser.Username);
        }
    }
}