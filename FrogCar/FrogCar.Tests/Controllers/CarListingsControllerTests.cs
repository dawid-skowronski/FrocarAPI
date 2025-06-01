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
    public class CarListingsControllerTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly CarListingsController _controller;

        public CarListingsControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
                .Options;
            _context = new AppDbContext(options);
            _notificationServiceMock = new Mock<INotificationService>();
            _controller = new CarListingsController(_context, _notificationServiceMock.Object);
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
        public async Task AddCarListing_ValidData_ReturnsOk()
        {
            SetupUser(1, "User");
            var carListing = new CarListing
            {
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                Features = new List<string> { "AC", "GPS" }
            };
            var admin = new User { Id = 2, Username = "admin", Email = "admin@example.com", Role = "Admin", Password = "Password123!" };
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            var result = await _controller.AddCarListing(carListing);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = okResult.Value;
            var messageProperty = returnValue.GetType().GetProperty("message");
            var carListingProperty = returnValue.GetType().GetProperty("carListing");
            var message = messageProperty.GetValue(returnValue);
            var returnedCarListing = carListingProperty.GetValue(returnValue) as CarListing;

            Assert.Equal("Ogłoszenie zostało dodane poprawnie, i oczekuje na zatwierdzenie przez Administratora", message);
            Assert.Equal("Toyota", returnedCarListing.Brand);
            Assert.Equal(1, returnedCarListing.UserId);
            Assert.True(returnedCarListing.IsAvailable);
            Assert.False(returnedCarListing.IsApproved);
        }

        [Fact]
        public async Task AddCarListing_Unauthenticated_ReturnsUnauthorized()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };
            var carListing = new CarListing { Brand = "Toyota" };

            var result = await _controller.AddCarListing(carListing);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var returnValue = unauthorizedResult.Value;
            var messageProperty = returnValue.GetType().GetProperty("message");
            var message = messageProperty.GetValue(returnValue);
            Assert.Equal("Musisz być zalogowany, aby dodać ogłoszenie.", message);
        }

        [Fact]
        public async Task ApproveListing_AsAdmin_ReturnsOk()
        {
            SetupUser(1, "Admin");
            var listing = new CarListing
            {
                Id = 1,
                UserId = 2,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50,
                IsApproved = false
            };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var result = await _controller.ApproveListing(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = okResult.Value;
            var messageProperty = returnValue.GetType().GetProperty("message");
            var listingProperty = returnValue.GetType().GetProperty("listing");
            var message = messageProperty.GetValue(returnValue);
            var returnedListing = listingProperty.GetValue(returnValue) as CarListing;

            Assert.Equal("Ogłoszenie zostało zatwierdzone.", message);
            Assert.True(returnedListing.IsApproved);

            var notification = await _context.Notifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(2, notification.UserId);
            Assert.Equal("Twoje ogłoszenie zostało zatwierdzone przez administratora.", notification.Message);
        }

        [Fact]
        public async Task ApproveListing_AsNonAdmin_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var listing = new CarListing { Id = 1, UserId = 1 };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var result = await _controller.ApproveListing(1);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Brak uprawnień do zatwierdzenia ogłoszenia. Tylko administrator może to zrobić.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateCarAvailability_AsOwner_ReturnsOk()
        {
            SetupUser(1, "User");
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                IsAvailable = true
            };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var result = await _controller.UpdateCarAvailability(1, false);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = okResult.Value;
            var messageProperty = returnValue.GetType().GetProperty("message");
            var listingProperty = returnValue.GetType().GetProperty("listing");
            var message = messageProperty.GetValue(returnValue);
            var returnedListing = listingProperty.GetValue(returnValue) as CarListing;

            Assert.Equal("Status dostępności został zmieniony.", message);
            Assert.False(returnedListing.IsAvailable);
        }

        [Fact]
        public async Task UpdateCarAvailability_AsNonOwner_ReturnsBadRequest()
        {
            SetupUser(2, "User");
            var listing = new CarListing { Id = 1, UserId = 1, IsAvailable = true };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var result = await _controller.UpdateCarAvailability(1, false);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("To nie jest Twoje ogłoszenie. Tylko właściciel lub administrator może zmieniać dostępność.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetUserCarListings_HasListings_ReturnsOk()
        {
            SetupUser(1, "User");
            var listings = new List<CarListing>
            {
                new CarListing { Id = 1, UserId = 1, Brand = "Toyota", IsApproved = true },
                new CarListing { Id = 2, UserId = 1, Brand = "Honda", IsApproved = true }
            };
            _context.CarListing.AddRange(listings);
            await _context.SaveChangesAsync();

            var result = await _controller.GetUserCarListings();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedListings = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(2, returnedListings.Count);
            Assert.Contains(returnedListings, l => l.Brand == "Toyota");
            Assert.Contains(returnedListings, l => l.Brand == "Honda");
        }

        [Fact]
        public async Task GetUserCarListings_NoListings_ReturnsNotFound()
        {
            SetupUser(1, "User");

            var result = await _controller.GetUserCarListings();

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Brak zatwierdzonych ogłoszeń dla tego użytkownika.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetAllCarListings_WithLocation_ReturnsFilteredListings()
        {
            SetupUser(1, "User");
            var listings = new List<CarListing>
            {
                new CarListing { Id = 1, UserId = 2, Brand = "Toyota", IsApproved = true, Latitude = 50.0, Longitude = 20.0 },
                new CarListing { Id = 2, UserId = 2, Brand = "Honda", IsApproved = true, Latitude = 51.0, Longitude = 21.0 }
            };
            _context.CarListing.AddRange(listings);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllCarListings(50.0, 20.0, 100);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedListings = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Single(returnedListings); 
            Assert.Equal("Toyota", returnedListings[0].Brand);
        }
        [Fact]
        public async Task GetAllCarListings_NoLocation_ReturnsAllListings()
        {
            SetupUser(1, "User");
            var listings = new List<CarListing>
    {
        new CarListing { Id = 1, UserId = 2, Brand = "Toyota", IsApproved = true },
        new CarListing { Id = 2, UserId = 2, Brand = "Honda", IsApproved = true }
    };
            _context.CarListing.AddRange(listings);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllCarListings(null, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedListings = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(2, returnedListings.Count);
        }

        [Fact]
        public async Task GetCarListing_Exists_ReturnsOk()
        {
            var listing = new CarListing { Id = 1, Brand = "Toyota" };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var result = await _controller.GetCarListing(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedListing = Assert.IsType<CarListing>(okResult.Value);
            Assert.Equal("Toyota", returnedListing.Brand);
        }
        [Fact]
        public async Task GetCarListing_DoesNotExist_ReturnsNotFound()
        {
            var result = await _controller.GetCarListing(1);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Ogłoszenie nie istnieje.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteCarListing_AsOwner_ReturnsOk()
        {
            SetupUser(1, "User");
            var listing = new CarListing { Id = 1, UserId = 1, Brand = "Toyota" };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCarListing(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Ogłoszenie usunięte.", okResult.Value);
            Assert.Empty(_context.CarListing);
        }
        [Fact]
        public async Task DeleteCarListing_AsNonOwner_ReturnsForbid()
        {
            SetupUser(2, "User");
            var listing = new CarListing { Id = 1, UserId = 1, Brand = "Toyota" };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCarListing(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateCarListing_AsOwner_ReturnsOk()
        {
            SetupUser(1, "User");
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50
            };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var updatedListing = new CarListing
            {
                Brand = "Honda",
                EngineCapacity = 1.8,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 40,
                Features = new List<string> { "AC" }
            };

            var result = await _controller.UpdateCarListing(1, updatedListing);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = okResult.Value;
            var messageProperty = returnValue.GetType().GetProperty("message");
            var listingProperty = returnValue.GetType().GetProperty("listing");
            var message = messageProperty.GetValue(returnValue);
            var returnedListing = listingProperty.GetValue(returnValue) as CarListing;

            Assert.Equal("Ogłoszenie zostało zaktualizowane.", message);
            Assert.Equal("Honda", returnedListing.Brand);
            Assert.Equal(1.8, returnedListing.EngineCapacity);
        }




        [Fact]
        public async Task UpdateCarListing_AsNonOwner_ReturnsBadRequest()
        {
            SetupUser(2, "User");
            var listing = new CarListing { Id = 1, UserId = 1, Brand = "Toyota" };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var updatedListing = new CarListing { Brand = "Honda" };

            var result = await _controller.UpdateCarListing(1, updatedListing);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("To nie jest Twoje ogłoszenie. Tylko właściciel może je edytować.", badRequestResult.Value);
        }
        [Fact]
        public async Task AddCarListing_EmptyFeatures_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var carListing = new CarListing
            {
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                Features = new List<string> { "" } 
            };

            var result = await _controller.AddCarListing(carListing);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Każda cecha musi być wypełniona poprawnie.", badRequestResult.Value);
        }

        [Fact]
        public async Task AddCarListing_NegativePrice_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var carListing = new CarListing
            {
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = -50m 
            };

            var result = await _controller.AddCarListing(carListing);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Cena wynajmu na jeden dzień musi być większa niż 0.", badRequestResult.Value);
        }

        [Fact]
        public async Task AddCarListing_NullBrand_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var carListing = new CarListing
            {
                Brand = null,
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m
            };

            var result = await _controller.AddCarListing(carListing);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Marka samochodu jest wymagana.", badRequestResult.Value);
        }

        [Fact]
        public async Task ApproveListing_NonExistentListing_ReturnsNotFound()
        {
            SetupUser(1, "Admin");

            var result = await _controller.ApproveListing(999);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Ogłoszenie nie istnieje.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateCarAvailability_AsAdmin_ReturnsOk()
        {
            SetupUser(2, "Admin");
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsAvailable = true
            };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var result = await _controller.UpdateCarAvailability(1, false);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = okResult.Value;
            var messageProperty = returnValue.GetType().GetProperty("message");
            var listingProperty = returnValue.GetType().GetProperty("listing");
            var message = messageProperty.GetValue(returnValue);
            var returnedListing = listingProperty.GetValue(returnValue) as CarListing;

            Assert.Equal("Status dostępności został zmieniony.", message);
            Assert.False(returnedListing.IsAvailable);
        }

        [Fact]
        public async Task GetAllCarListings_NoListingsInRegion_ReturnsNotFound()
        {
            SetupUser(1, "User");
            var listings = new List<CarListing>
            {
                new CarListing { Id = 1, UserId = 2, Brand = "Toyota", EngineCapacity = 2.0, FuelType = "Petrol", CarType = "Sedan", Seats = 5, RentalPricePerDay = 50m, IsApproved = true, Latitude = 50.0, Longitude = 20.0 }
            };
            _context.CarListing.AddRange(listings);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllCarListings(60.0, 30.0, 10);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Brak dostępnych samochodów w podanym regionie.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteCarListing_AsAdmin_ReturnsOk()
        {
            SetupUser(2, "Admin");
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m
            };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCarListing(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Ogłoszenie usunięte.", okResult.Value);
            Assert.Empty(_context.CarListing);
        }

        [Fact]
        public async Task UpdateCarListing_NegativeSeats_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m
            };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var updatedListing = new CarListing
            {
                Brand = "Honda",
                EngineCapacity = 1.8,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = -1,
                RentalPricePerDay = 40m
            };

            var result = await _controller.UpdateCarListing(1, updatedListing);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Liczba miejsc musi być większa od 0.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateCarListing_EmptyFuelType_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m
            };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var updatedListing = new CarListing
            {
                Brand = "Honda",
                EngineCapacity = 1.8,
                FuelType = "", 
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 40m
            };

            var result = await _controller.UpdateCarListing(1, updatedListing);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Typ paliwa jest wymagany.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetAllCarListings_VerifiesDistanceCalculation()
        {
            SetupUser(1, "User");
            var listings = new List<CarListing>
            {
                new CarListing { Id = 1, UserId = 2, Brand = "Toyota", EngineCapacity = 2.0, FuelType = "Petrol", CarType = "Sedan", Seats = 5, RentalPricePerDay = 50m, IsApproved = true, Latitude = 50.0, Longitude = 20.0 },
                new CarListing { Id = 2, UserId = 2, Brand = "Honda", EngineCapacity = 1.8, FuelType = "Petrol", CarType = "Sedan", Seats = 5, RentalPricePerDay = 40m, IsApproved = true, Latitude = 50.01, Longitude = 20.01 } // Bardzo blisko
            };
            _context.CarListing.AddRange(listings);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllCarListings(50.0, 20.0, 2);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedListings = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(2, returnedListings.Count);
            Assert.Contains(returnedListings, l => l.Brand == "Toyota");
            Assert.Contains(returnedListings, l => l.Brand == "Honda");
        }

        [Fact]
        public async Task DeleteCarListing_NonExistent_ReturnsNotFound()
        {
            SetupUser(1, "User");

            var result = await _controller.DeleteCarListing(999);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Ogłoszenie nie istnieje.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateCarListing_NegativePrice_ReturnsBadRequest()
        {
            SetupUser(1, "User");
            var listing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m
            };
            _context.CarListing.Add(listing);
            await _context.SaveChangesAsync();

            var updatedListing = new CarListing
            {
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = -10m 
            };

            var result = await _controller.UpdateCarListing(1, updatedListing);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Cena wynajmu na jeden dzień musi być większa niż 0.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetAllCarListings_OnlyApprovedListings_ReturnsApprovedOnly()
        {
            SetupUser(1, "User");
            var listings = new List<CarListing>
            {
                new CarListing
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
                    IsAvailable = true,
                    Latitude = 50.0,
                    Longitude = 20.0
                },
                new CarListing
                {
                    Id = 2,
                    UserId = 2,
                    Brand = "Honda",
                    EngineCapacity = 1.8,
                    FuelType = "Petrol",
                    CarType = "Hatchback",
                    Seats = 5,
                    RentalPricePerDay = 40m,
                    IsApproved = false,
                    IsAvailable = true,
                    Latitude = 50.0,
                    Longitude = 20.0
                }
            };
            _context.CarListing.AddRange(listings);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllCarListings(50.0, 20.0, 100);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedListings = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Single(returnedListings);
            Assert.Equal("Toyota", returnedListings[0].Brand);
            Assert.True(returnedListings[0].IsApproved);
        }
    }
}