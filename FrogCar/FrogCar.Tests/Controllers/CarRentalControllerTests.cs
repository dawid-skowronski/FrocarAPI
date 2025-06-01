using FrogCar.Controllers;
using FrogCar.Data;
using FrogCar.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace FrogCar.Tests.Controllers
{
    public class CarRentalControllerTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly CarRentalController _controller;

        public CarRentalControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _notificationServiceMock = new Mock<INotificationService>();
            _controller = new CarRentalController(_context, _notificationServiceMock.Object);
        }

        private void SetupUser(int userId, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task CreateCarRental_ValidData_ReturnsOk()
        {
            SetupUser(1, "User");
            var carListing = new CarListing { Id = 1, UserId = 2, Brand = "Toyota", IsAvailable = true, IsApproved = true, RentalPricePerDay = 50 };
            var user = new User { Id = 1, Username = "user1", Email = "user1@example.com", Password = "Password123!" };
            _context.CarListing.Add(carListing);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var carRentalRequest = new CarRentalRequest
            {
                CarListingId = 1,
                RentalStartDate = DateTime.Now,
                RentalEndDate = DateTime.Now.AddDays(1)
            };

            var result = await _controller.CreateCarRental(carRentalRequest);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = okResult.Value;
            var messageProperty = returnValue.GetType().GetProperty("message");
            var carRentalProperty = returnValue.GetType().GetProperty("carRental");
            var message = messageProperty.GetValue(returnValue);
            var carRental = carRentalProperty.GetValue(returnValue) as CarRental;

            Assert.Equal("Wypożyczenie zostało dodane.", message);
            Assert.Equal(1, carRental.CarListingId);
        }





        [Fact]
        public async Task GetUserCarRentals_HasRentals_ReturnsOk()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true,
                IsAvailable = true
            };
            var rentals = new List<CarRental>
    {
        new CarRental
        {
            CarRentalId = 1,
            UserId = 1,
            User = user,
            CarListingId = 1,
            CarListing = carListing,
            RentalStatus = "Aktywne",
            RentalStartDate = DateTime.UtcNow,
            RentalEndDate = DateTime.UtcNow.AddDays(1),
            RentalPrice = 50m
        },
        new CarRental
        {
            CarRentalId = 2,
            UserId = 1,
            User = user,
            CarListingId = 1,
            CarListing = carListing,
            RentalStatus = "Aktywne",
            RentalStartDate = DateTime.UtcNow,
            RentalEndDate = DateTime.UtcNow.AddDays(1),
            RentalPrice = 50m
        }
    };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.AddRange(rentals);
            await _context.SaveChangesAsync();

            var result = await _controller.GetUserCarRentals();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRentals = Assert.IsAssignableFrom<List<CarRental>>(okResult.Value);
            Assert.Equal(2, returnedRentals.Count);
            Assert.All(returnedRentals, r => Assert.Equal("Aktywne", r.RentalStatus));
            Assert.All(returnedRentals, r => Assert.Equal(1, r.UserId));
        }





        [Fact]
        public async Task UpdateCarRentalStatus_AsOwner_ReturnsOk()
        {
            SetupUser(1, "User");
            var rental = new CarRental { CarRentalId = 1, UserId = 1, RentalStatus = "Aktywne" };
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.UpdateCarRentalStatus(1, "Zakończone");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = okResult.Value;
            var messageProperty = returnValue.GetType().GetProperty("message");
            var rentalProperty = returnValue.GetType().GetProperty("rental");
            var message = messageProperty.GetValue(returnValue);
            var updatedRental = rentalProperty.GetValue(returnValue) as CarRental;

            Assert.Equal("Status wypożyczenia został zmieniony.", message);
            Assert.Equal("Zakończone", updatedRental.RentalStatus);
        }




        [Fact]
        public async Task AddReview_ValidData_ReturnsOk()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true,
                IsAvailable = true
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Zakończone",
                RentalStartDate = DateTime.UtcNow.AddDays(-2),
                RentalEndDate = DateTime.UtcNow.AddDays(-1),
                RentalPrice = 100m
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var reviewRequest = new CarRentalReviewRequest
            {
                CarRentalId = 1,
                Rating = 5,
                Comment = "Great car!"
            };

            var result = await _controller.AddReview(reviewRequest);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = okResult.Value;
            Assert.NotNull(returnValue);

            var messageProperty = returnValue.GetType().GetProperty("message");
            var reviewProperty = returnValue.GetType().GetProperty("review");
            Assert.NotNull(messageProperty);
            Assert.NotNull(reviewProperty);

            var message = messageProperty.GetValue(returnValue);
            var review = (CarRentalReview)reviewProperty.GetValue(returnValue);

            Assert.Equal("Recenzja została dodana.", message);
            Assert.Equal(5, review.Rating);
            Assert.Equal("Great car!", review.Comment);
            Assert.Equal(1, review.CarRentalId);
            Assert.Equal(1, review.UserId);

            var listing = await _context.CarListing.FindAsync(1);
            Assert.Equal(5.0, listing.AverageRating);
        }


        [Fact]
        public async Task DeleteCarRental_AsOwner_ReturnsOk()
        {
            SetupUser(1, "User");
            var carListing = new CarListing { Id = 1, UserId = 2, Brand = "Toyota", IsAvailable = false };
            var rental = new CarRental { CarRentalId = 1, UserId = 1, CarListingId = 1, CarListing = carListing, RentalStatus = "Aktywne" };
            _context.CarRentals.Add(rental);
            _context.CarListing.Add(carListing);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCarRental(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Wypożyczenie zostało usunięte i samochód jest teraz dostępny.", okResult.Value);
            Assert.Empty(_context.CarRentals);
        }
        [Fact]
        public async Task GetAllCarRentals_HasRentals_ReturnsOk()
        {
            SetupUser(1, "Admin");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true,
                IsAvailable = false
            };
            var rentals = new List<CarRental>
            {
                new CarRental
                {
                    CarRentalId = 1,
                    UserId = 1,
                    User = user,
                    CarListingId = 1,
                    CarListing = carListing,
                    RentalStatus = "Aktywne",
                    RentalStartDate = DateTime.UtcNow,
                    RentalEndDate = DateTime.UtcNow.AddDays(1),
                    RentalPrice = 50m
                }
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.AddRange(rentals);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllCarRentals();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRentals = Assert.IsAssignableFrom<List<CarRental>>(okResult.Value);
            Assert.Single(returnedRentals);
            Assert.Equal("Aktywne", returnedRentals[0].RentalStatus);
            Assert.Equal(1, returnedRentals[0].CarRentalId);
        }

        [Fact]
        public async Task GetAllCarRentals_NoRentals_ReturnsNotFound()
        {
            SetupUser(1, "Admin");

            var result = await _controller.GetAllCarRentals();

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Brak wypożyczeń.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetCarRental_Exists_ReturnsOk()
        {

            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                User = user,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Aktywne",
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1),
                RentalPrice = 50m
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.GetCarRental(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRental = Assert.IsType<CarRental>(okResult.Value);
            Assert.Equal(1, returnedRental.CarRentalId);
            Assert.Equal("Aktywne", returnedRental.RentalStatus);
        }

        [Fact]
        public async Task GetCarRental_NotExists_ReturnsNotFound()
        {
            SetupUser(1, "User");

            var result = await _controller.GetCarRental(1);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Wypożyczenie nie istnieje.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetUserCarRentalHistory_HasEndedRentals_ReturnsOk()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true
            };
            var rentals = new List<CarRental>
            {
                new CarRental
                {
                    CarRentalId = 1,
                    UserId = 1,
                    User = user,
                    CarListingId = 1,
                    CarListing = carListing,
                    RentalStatus = "Zakończone",
                    RentalStartDate = DateTime.UtcNow.AddDays(-2),
                    RentalEndDate = DateTime.UtcNow.AddDays(-1),
                    RentalPrice = 50m
                }
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.AddRange(rentals);
            await _context.SaveChangesAsync();

            var result = await _controller.GetUserCarRentalHistory();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRentals = Assert.IsAssignableFrom<List<CarRental>>(okResult.Value);
            Assert.Single(returnedRentals);
            Assert.Equal("Zakończone", returnedRentals[0].RentalStatus);
        }

        [Fact]
        public async Task GetUserCarRentalHistory_NoEndedRentals_ReturnsNotFound()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _controller.GetUserCarRentalHistory();

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Brak zakończonych wypożyczeń dla tego użytkownika.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetReviewsForListing_HasReviews_ReturnsOk()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Zakończone",
                RentalStartDate = DateTime.UtcNow.AddDays(-2),
                RentalEndDate = DateTime.UtcNow.AddDays(-1),
                RentalPrice = 50m
            };
            var review = new CarRentalReview
            {
                ReviewId = 1,
                CarRentalId = 1,
                UserId = 1,
                Rating = 5,
                Comment = "Great car!",
                CarRental = rental,
                User = user
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            _context.CarRentalReviews.Add(review);
            await _context.SaveChangesAsync();

            var result = await _controller.GetReviewsForListing(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReviews = Assert.IsAssignableFrom<List<CarRentalReview>>(okResult.Value);
            Assert.Single(returnedReviews);
            Assert.Equal(5, returnedReviews[0].Rating);
            Assert.Equal("Great car!", returnedReviews[0].Comment);
        }

        [Fact]
        public async Task GetReviewsForListing_NoReviews_ReturnsNotFound()
        {
            SetupUser(1, "User");
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true
            };
            _context.CarListing.Add(carListing);
            await _context.SaveChangesAsync();

            var result = await _controller.GetReviewsForListing(1);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Brak recenzji dla tego ogłoszenia.", notFoundResult.Value);
        }

        [Fact]
        public async Task CreateCarRental_InvalidDates_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true,
                IsAvailable = true
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            await _context.SaveChangesAsync();

            var carRentalRequest = new CarRentalRequest
            {
                CarListingId = 1,
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(-1) 
            };

            var result = await _controller.CreateCarRental(carRentalRequest);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Data zakończenia wypożyczenia musi być późniejsza niż data rozpoczęcia.", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateCarRental_OwnCar_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true,
                IsAvailable = true
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            await _context.SaveChangesAsync();

            var carRentalRequest = new CarRentalRequest
            {
                CarListingId = 1,
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1)
            };

            var result = await _controller.CreateCarRental(carRentalRequest);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nie możesz wypożyczyć własnego samochodu.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateCarRentalStatus_AsNonOwnerNonAdmin_ReturnsBadRequest()
        {
            SetupUser(2, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 3,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Aktywne",
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1),
                RentalPrice = 50m
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.UpdateCarRentalStatus(1, "Zakończone");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("To nie jest Twoje wypożyczenie. Tylko właściciel lub administrator może zmieniać status.", badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteCarRental_AsNonOwnerNonAdmin_ReturnsUnauthorized()
        {
            SetupUser(2, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 3,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true,
                IsAvailable = false
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Aktywne",
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1),
                RentalPrice = 50m
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCarRental(1);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var returnValue = unauthorizedResult.Value;
            var messageProperty = returnValue.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(returnValue);
            Assert.Equal("Nie masz uprawnień do usunięcia tego wypożyczenia.", message);
        }

        [Fact]
        public async Task AddReview_InvalidRating_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Zakończone",
                RentalStartDate = DateTime.UtcNow.AddDays(-2),
                RentalEndDate = DateTime.UtcNow.AddDays(-1),
                RentalPrice = 50m
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var reviewRequest = new CarRentalReviewRequest
            {
                CarRentalId = 1,
                Rating = 6, 
                Comment = "Great car!"
            };

            var result = await _controller.AddReview(reviewRequest);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Ocena musi być w zakresie 1-5.", badRequestResult.Value);
        }
        [Fact]
        public async Task CreateCarRental_NullRequest_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            CarRentalRequest carRentalRequest = null;

            var result = await _controller.CreateCarRental(carRentalRequest);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Dane wypożyczenia są wymagane.", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateCarRental_NonExistentCar_ReturnsNotFound()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var carRentalRequest = new CarRentalRequest
            {
                CarListingId = 999,
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1)
            };

            var result = await _controller.CreateCarRental(carRentalRequest);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Samochód nie istnieje.", notFoundResult.Value);
        }

        [Fact]
        public async Task CreateCarRental_NonApprovedCar_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = false,
                IsAvailable = true
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            await _context.SaveChangesAsync();

            var carRentalRequest = new CarRentalRequest
            {
                CarListingId = 1,
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1)
            };

            var result = await _controller.CreateCarRental(carRentalRequest);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Samochód jest nie dostępny.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetUserCarRentals_NoActiveRentals_ReturnsNotFound()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Zakończone",
                RentalStartDate = DateTime.UtcNow.AddDays(-2),
                RentalEndDate = DateTime.UtcNow.AddDays(-1),
                RentalPrice = 50m
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.GetUserCarRentals();

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Brak aktywnych wypożyczeń dla tego użytkownika.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateCarRentalStatus_AsAdmin_ReturnsOk()
        {
            SetupUser(2, "Admin");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 3,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Aktywne",
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1),
                RentalPrice = 50m
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.UpdateCarRentalStatus(1, "Zakończone");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = okResult.Value;
            var messageProperty = returnValue.GetType().GetProperty("message");
            var rentalProperty = returnValue.GetType().GetProperty("rental");
            var message = messageProperty.GetValue(returnValue);
            var updatedRental = rentalProperty.GetValue(returnValue) as CarRental;

            Assert.Equal("Status wypożyczenia został zmieniony.", message);
            Assert.Equal("Zakończone", updatedRental.RentalStatus);
        }

        [Fact]
        public async Task AddReview_NonExistentRental_ReturnsNotFound()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var reviewRequest = new CarRentalReviewRequest
            {
                CarRentalId = 999,
                Rating = 5,
                Comment = "Great car!"
            };

            var result = await _controller.AddReview(reviewRequest);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Nie znaleziono wypożyczenia.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteCarRental_AsAdmin_ReturnsOk()
        {
            SetupUser(2, "Admin");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 3,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true,
                IsAvailable = false
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Aktywne",
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1),
                RentalPrice = 50m
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCarRental(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Wypożyczenie zostało usunięte i samochód jest teraz dostępny.", okResult.Value);
            Assert.Empty(_context.CarRentals);
            var updatedListing = await _context.CarListing.FindAsync(1);
            Assert.True(updatedListing.IsAvailable);
        }

        [Fact]
        public async Task DeleteCarRental_SendsNoNotification()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true,
                IsAvailable = false
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Aktywne",
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1),
                RentalPrice = 50m
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCarRental(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Wypożyczenie zostało usunięte i samochód jest teraz dostępny.", okResult.Value);
            Assert.Empty(_context.Notifications); 
        }
        [Fact]
        public async Task GetUserCarRentalHistory_OnlyActiveRentals_ReturnsNotFound()
        {
            SetupUser(1, "User");
            var user = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = true
            };
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                CarListingId = 1,
                CarListing = carListing,
                RentalStatus = "Aktywne",
                RentalStartDate = DateTime.UtcNow,
                RentalEndDate = DateTime.UtcNow.AddDays(1),
                RentalPrice = 50m
            };
            _context.Users.Add(user);
            _context.CarListing.Add(carListing);
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            var result = await _controller.GetUserCarRentalHistory();

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Brak zakończonych wypożyczeń dla tego użytkownika.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteCarRental_NonExistent_ReturnsNotFound()
        {
            SetupUser(1, "User");

            var result = await _controller.DeleteCarRental(999);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Wypożyczenie nie istnieje.", notFoundResult.Value);
        }
    }
}
