using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FrogCar.Data;
using FrogCar.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FrogCar.Constants;
using FrogCar.Controllers;
using System.Collections.Generic;
using System.Text.Json;

namespace FrogCar.Tests.Controllers;
public class CarListingsControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CarListingsController _controller;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<CarListingsController>> _mockLogger;

    public CarListingsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<CarListingsController>>();

        _controller = new CarListingsController(_context, _mockNotificationService.Object, _mockLogger.Object);

        ClearDatabase();
    }

    private void ClearDatabase()
    {
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    private void SetControllerUser(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddCarListing_ValidListing_ReturnsOkResultAndAddsListing()
    {
        ClearDatabase();
        SetControllerUser(1, Roles.User);
        var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Password = "hashedpassword", Role = Roles.User };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var carListing = new CarListing
        {
            Brand = "Toyota",
            EngineCapacity = 2000,
            Seats = 5,
            FuelType = "Gasoline",
            CarType = "Sedan",
            RentalPricePerDay = 50.00m,
            Features = new List<string> { "AC", "GPS" },
            Latitude = 51.107883,
            Longitude = 17.038538
        };

        var result = await _controller.AddCarListing(carListing);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(okResult.Value));

        Assert.Equal("Ogłoszenie zostało dodane i oczekuje na zatwierdzenie.", jsonElement.GetProperty("message").GetString());
        Assert.True(jsonElement.GetProperty("id").GetInt32() > 0);

        var addedListing = await _context.CarListing.FirstOrDefaultAsync(l => l.Id == jsonElement.GetProperty("id").GetInt32());
        Assert.NotNull(addedListing);
        Assert.Equal(carListing.Brand, addedListing.Brand);
        Assert.Equal(1, addedListing.UserId);
        Assert.True(addedListing.IsAvailable);
        Assert.False(addedListing.IsApproved);
        Assert.Equal(carListing.RentalPricePerDay, addedListing.RentalPricePerDay);
    }

    [Theory]
    [InlineData(null, 1.5, 5, "Gasoline", "SUV", 100.00, ErrorMessages.BadRequestRequiredBrand)]
    [InlineData("Brand", 0, 5, "Gasoline", "SUV", 100.00, ErrorMessages.BadRequestEngineCapacity)]
    [InlineData("Brand", 1.5, 0, "Gasoline", "SUV", 100.00, ErrorMessages.BadRequestSeats)]
    [InlineData("Brand", 1.5, 5, null, "SUV", 100.00, ErrorMessages.BadRequestFuelType)]
    [InlineData("Brand", 1.5, 5, "Gasoline", null, 100.00, ErrorMessages.BadRequestCarType)]
    [InlineData("Brand", 1.5, 5, "Gasoline", "SUV", 0.00, ErrorMessages.BadRequestRentalPrice)]
    [InlineData("Brand", 1.5, 5, "Gasoline", "SUV", 100.00, ErrorMessages.BadRequestInvalidFeature, "AC", null)]
    public async Task AddCarListing_InvalidListingData_ReturnsBadRequest(
        string brand, double engineCapacity, int seats, string fuelType, string carType,
        decimal rentalPricePerDay, string expectedErrorMessage,
        string feature1 = "AC", string feature2 = "GPS")
    {
        ClearDatabase();
        SetControllerUser(1, Roles.User);
        var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Password = "hashedpassword", Role = Roles.User };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var features = new List<string>();
        if (feature1 != null) features.Add(feature1);
        if (feature2 != null) features.Add(feature2);

        var carListing = new CarListing
        {
            Brand = brand,
            EngineCapacity = engineCapacity,
            Seats = seats,
            FuelType = fuelType,
            CarType = carType,
            RentalPricePerDay = rentalPricePerDay,
            Features = features
        };

        if (expectedErrorMessage == ErrorMessages.BadRequestInvalidFeature)
        {
            carListing.Features.Add(null);
        }

        var result = await _controller.AddCarListing(carListing);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(badRequestResult.Value));
        Assert.Equal(expectedErrorMessage, jsonElement.GetProperty("message").GetString());

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Niepoprawne dane ogłoszenia: {expectedErrorMessage}")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task AddCarListing_NullListing_ReturnsBadRequest()
    {
        ClearDatabase();
        SetControllerUser(1, Roles.User);
        var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Password = "hashedpassword", Role = Roles.User };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _controller.AddCarListing(null);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(badRequestResult.Value));
        Assert.Equal(ErrorMessages.BadRequestEmptyListing, jsonElement.GetProperty("message").GetString());

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Niepoprawne dane ogłoszenia: {ErrorMessages.BadRequestEmptyListing}")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task ApproveListing_AdminApprovesValidListing_ReturnsOkResultAndApproves()
    {
        ClearDatabase();
        SetControllerUser(2, Roles.Admin);
        var owner = new User { Id = 1, Username = "owner", Email = "owner@e.com", Password = "p", Role = Roles.User };
        var admin = new User { Id = 2, Username = "admin", Email = "admin@e.com", Password = "p", Role = Roles.Admin };
        var listing = new CarListing { Id = 1, UserId = 1, Brand = "Ford", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsApproved = false, IsAvailable = true };
        _context.Users.AddRange(owner, admin);
        _context.CarListing.Add(listing);
        await _context.SaveChangesAsync();

        var result = await _controller.ApproveListing(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(okResult.Value));

        Assert.Equal("Ogłoszenie zostało zatwierdzone.", jsonElement.GetProperty("message").GetString());
        var returnedListing = Assert.IsType<CarListing>(okResult.Value.GetType().GetProperty("listing").GetValue(okResult.Value));
        Assert.True(returnedListing.IsApproved);

        var updatedListing = await _context.CarListing.FindAsync(1);
        Assert.True(updatedListing.IsApproved);
    }

    [Fact]
    public async Task ApproveListing_AdminApprovesNonExistentListing_ReturnsNotFound()
    {
        ClearDatabase();
        SetControllerUser(2, Roles.Admin);

        var result = await _controller.ApproveListing(999);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(notFoundResult.Value));
        Assert.Equal(ErrorMessages.ListingNotFound, jsonElement.GetProperty("message").GetString());

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbował zatwierdzić nieistniejące ogłoszenie ID: 999")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task ApproveListing_ListingAlreadyApproved_ReturnsBadRequest()
    {
        ClearDatabase();
        SetControllerUser(2, Roles.Admin);
        var listing = new CarListing { Id = 1, UserId = 1, Brand = "Ford", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsApproved = true, IsAvailable = true };
        _context.CarListing.Add(listing);
        await _context.SaveChangesAsync();

        var result = await _controller.ApproveListing(1);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(badRequestResult.Value));
        Assert.Equal("Ogłoszenie jest już zatwierdzone.", jsonElement.GetProperty("message").GetString());

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbował zatwierdzić już zatwierdzone ogłoszenie ID: 1")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCarAvailability_OwnerUpdatesAvailability_ReturnsOkResultAndUpdates()
    {
        ClearDatabase();
        SetControllerUser(1, Roles.User);
        var listing = new CarListing { Id = 1, UserId = 1, Brand = "Ford", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsAvailable = true, IsApproved = true };
        _context.CarListing.Add(listing);
        await _context.SaveChangesAsync();

        var result = await _controller.UpdateCarAvailability(1, false);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(okResult.Value));
        Assert.Equal("Status dostępności został zmieniony.", jsonElement.GetProperty("message").GetString());
        var returnedListing = Assert.IsType<CarListing>(okResult.Value.GetType().GetProperty("listing").GetValue(okResult.Value));
        Assert.False(returnedListing.IsAvailable);

        var updatedListing = await _context.CarListing.FindAsync(1);
        Assert.False(updatedListing.IsAvailable);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Dostępność ogłoszenia ID: 1 zmieniono na False przez użytkownika ID: 1.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCarAvailability_AdminUpdatesAvailability_ReturnsOkResultAndUpdates()
    {
        ClearDatabase();
        SetControllerUser(2, Roles.Admin);
        var listing = new CarListing { Id = 1, UserId = 1, Brand = "Ford", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsAvailable = true, IsApproved = true };
        _context.CarListing.Add(listing);
        await _context.SaveChangesAsync();

        var result = await _controller.UpdateCarAvailability(1, false);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(okResult.Value));
        Assert.Equal("Status dostępności został zmieniony.", jsonElement.GetProperty("message").GetString());
        var returnedListing = Assert.IsType<CarListing>(okResult.Value.GetType().GetProperty("listing").GetValue(okResult.Value));
        Assert.False(returnedListing.IsAvailable);

        var updatedListing = await _context.CarListing.FindAsync(1);
        Assert.False(updatedListing.IsAvailable);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Dostępność ogłoszenia ID: 1 zmieniono na False przez użytkownika ID: 2.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCarAvailability_NonOwnerNonAdmin_ReturnsUnauthorized()
    {
        ClearDatabase();
        SetControllerUser(3, Roles.User);
        var listing = new CarListing { Id = 1, UserId = 1, Brand = "Ford", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsAvailable = true, IsApproved = true };
        _context.CarListing.Add(listing);
        await _context.SaveChangesAsync();

        var result = await _controller.UpdateCarAvailability(1, false);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(unauthorizedResult.Value));
        Assert.Equal(ErrorMessages.NotOwnerOrAdmin, jsonElement.GetProperty("message").GetString());

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Użytkownik ID: 3 próbował zmienić dostępność ogłoszenia ID: 1, do którego nie ma uprawnień.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCarAvailability_ListingNotFound_ReturnsNotFound()
    {
        ClearDatabase();
        SetControllerUser(1, Roles.User);

        var result = await _controller.UpdateCarAvailability(999, false);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(notFoundResult.Value));
        Assert.Equal(ErrorMessages.ListingNotFound, jsonElement.GetProperty("message").GetString());

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Użytkownik ID: 1 próbował zmienić dostępność nieistniejącego ogłoszenia ID: 999")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUserCarListings_UserHasApprovedListings_ReturnsOkResultWithListings()
    {
        ClearDatabase();
        SetControllerUser(1, Roles.User);
        var user = new User { Id = 1, Username = "owner", Email = "owner@e.com", Password = "p", Role = Roles.User };
        var listing1 = new CarListing { Id = 1, UserId = 1, Brand = "Ford", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsApproved = true, IsAvailable = true };
        var listing2 = new CarListing { Id = 2, UserId = 1, Brand = "BMW", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsApproved = true, IsAvailable = true };
        var listing3 = new CarListing { Id = 3, UserId = 2, Brand = "Audi", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsApproved = true, IsAvailable = true };
        var listing4 = new CarListing { Id = 4, UserId = 1, Brand = "Merc", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsApproved = false, IsAvailable = true };

        _context.Users.Add(user);
        _context.CarListing.AddRange(listing1, listing2, listing3, listing4);
        await _context.SaveChangesAsync();

        var result = await _controller.GetUserCarListings();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var listings = Assert.IsType<List<CarListing>>(okResult.Value);

        Assert.Equal(2, listings.Count);
        Assert.Contains(listings, l => l.Id == 1);
        Assert.Contains(listings, l => l.Id == 2);
        Assert.DoesNotContain(listings, l => l.Id == 3);
        Assert.DoesNotContain(listings, l => l.Id == 4);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pomyślnie pobrano 2 zatwierdzonych ogłoszeń dla użytkownika ID: 1.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUserCarListings_UserHasNoApprovedListings_ReturnsNotFound()
    {
        ClearDatabase();
        SetControllerUser(1, Roles.User);
        var user = new User { Id = 1, Username = "owner", Email = "owner@e.com", Password = "p", Role = Roles.User };
        var listing1 = new CarListing { Id = 1, UserId = 1, Brand = "Ford", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsApproved = false, IsAvailable = true };

        _context.Users.Add(user);
        _context.CarListing.Add(listing1);
        await _context.SaveChangesAsync();

        var result = await _controller.GetUserCarListings();

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(notFoundResult.Value));
        Assert.Equal(ErrorMessages.NoApprovedListingsForUser, jsonElement.GetProperty("message").GetString());

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Brak zatwierdzonych ogłoszeń dla użytkownika ID: 1.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUserCarListings_NoListingsAtAll_ReturnsNotFound()
    {
        ClearDatabase();
        SetControllerUser(1, Roles.User);

        var result = await _controller.GetUserCarListings();

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(notFoundResult.Value));
        Assert.Equal(ErrorMessages.NoApprovedListingsForUser, jsonElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetAllCarListings_WithLocationFilter_ReturnsListingsWithinRadius()
    {
        ClearDatabase();
        SetControllerUser(1, Roles.User);
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@e.com", Password = "p", Role = Roles.User };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@e.com", Password = "p", Role = Roles.User };
        var user3 = new User { Id = 3, Username = "user3", Email = "user3@e.com", Password = "p", Role = Roles.User };

        var listing1 = new CarListing { Id = 1, UserId = 2, Brand = "BMW", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsApproved = true, IsAvailable = true, Latitude = 52.2297, Longitude = 21.0122 };
        var listing2 = new CarListing { Id = 2, UserId = 3, Brand = "Audi", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsApproved = true, IsAvailable = true, Latitude = 50.0647, Longitude = 19.9450 };

        _context.Users.AddRange(user1, user2, user3);
        _context.CarListing.AddRange(listing1, listing2);
        await _context.SaveChangesAsync();

        double warsawLat = 52.2297;
        double warsawLng = 21.0122;
        double radiusKm = 10;

        var result = await _controller.GetAllCarListings(warsawLat, warsawLng, radiusKm);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var listings = Assert.IsType<List<CarListing>>(okResult.Value);

        Assert.Single(listings);
        Assert.Contains(listings, l => l.Id == 1);
        Assert.DoesNotContain(listings, l => l.Id == 2);
    }

    [Fact]
    public async Task GetAllCarListings_WithLocationFilter_NoListingsInRadius_ReturnsEmptyList()
    {
        ClearDatabase();
        SetControllerUser(1, Roles.User);
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@e.com", Password = "p", Role = Roles.User };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@e.com", Password = "p", Role = Roles.User };

        var listing1 = new CarListing { Id = 1, UserId = 2, Brand = "BMW", EngineCapacity = 1000, FuelType = "Gas", Seats = 4, CarType = "Sedan", RentalPricePerDay = 50.00m, IsApproved = true, IsAvailable = true, Latitude = 50.0647, Longitude = 19.9450 };

        _context.Users.AddRange(user1, user2);
        _context.CarListing.Add(listing1);
        await _context.SaveChangesAsync();

        double warsawLat = 52.2297;
        double warsawLng = 21.0122;
        double radiusKm = 10;

        var result = await _controller.GetAllCarListings(warsawLat, warsawLng, radiusKm);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var listings = Assert.IsType<List<CarListing>>(okResult.Value);

        Assert.Empty(listings);
    }
}