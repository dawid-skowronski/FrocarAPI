using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FrogCar.Data;
using FrogCar.Models;
using FrogCar.Constants;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using FrogCar.Controllers;

namespace FrogCar.Tests.Controllers;
public class CarRentalControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<CarRentalController>> _mockLogger;
    private readonly CarRentalController _controller;

    public CarRentalControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        SeedDatabase();

        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<CarRentalController>>();

        _controller = new CarRentalController(_context, _mockNotificationService.Object, _mockLogger.Object);
    }

    private void SeedDatabase()
    {
        _context.CarListing.RemoveRange(_context.CarListing);
        _context.Users.RemoveRange(_context.Users);
        _context.CarRentals.RemoveRange(_context.CarRentals);
        _context.CarRentalReviews.RemoveRange(_context.CarRentalReviews);
        _context.SaveChanges();

        _context.Users.Add(new User { Id = 1, Username = "testuser", Email = "test@example.com", Role = Roles.User, Password = "hashedpassword1" });
        _context.Users.Add(new User { Id = 2, Username = "carowner", Email = "owner@example.com", Role = Roles.User, Password = "hashedpassword2" });
        _context.Users.Add(new User { Id = 3, Username = "adminuser", Email = "admin@example.com", Role = Roles.Admin, Password = "hashedpassword3" });
        _context.Users.Add(new User { Id = 4, Username = "stranger", Email = "stranger@example.com", Role = Roles.User, Password = "hashedpassword4" });
        _context.SaveChanges();

        _context.CarListing.Add(new CarListing { Id = 101, Brand = "Toyota", EngineCapacity = 2.0, RentalPricePerDay = 50, IsAvailable = true, IsApproved = true, UserId = 2 });
        _context.CarListing.Add(new CarListing { Id = 102, Brand = "Honda", EngineCapacity = 1.8, RentalPricePerDay = 40, IsAvailable = false, IsApproved = true, UserId = 2 });
        _context.CarListing.Add(new CarListing { Id = 103, Brand = "Ford", EngineCapacity = 2.5, RentalPricePerDay = 60, IsAvailable = true, IsApproved = false, UserId = 2 });
        _context.CarListing.Add(new CarListing { Id = 104, Brand = "BMW", EngineCapacity = 3.0, RentalPricePerDay = 100, IsAvailable = true, IsApproved = true, UserId = 1 });
        _context.SaveChanges();

        _context.CarRentals.Add(new CarRental { CarRentalId = 1, CarListingId = 101, UserId = 1, RentalStartDate = DateTime.Now.AddDays(-10), RentalEndDate = DateTime.Now.AddDays(-5), RentalPrice = 250, RentalStatus = "Zakończone" });
        _context.CarRentals.Add(new CarRental { CarRentalId = 2, CarListingId = 101, UserId = 1, RentalStartDate = DateTime.Now.AddDays(-3), RentalEndDate = DateTime.Now.AddDays(2), RentalPrice = 250, RentalStatus = "Aktywne" });
        _context.CarRentals.Add(new CarRental { CarRentalId = 3, CarListingId = 102, UserId = 1, RentalStartDate = DateTime.Now.AddDays(-20), RentalEndDate = DateTime.Now.AddDays(-15), RentalPrice = 200, RentalStatus = "Anulowane" });
        _context.SaveChanges();
    }

    private void SetUserContext(int userId, string role)
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
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void AssertBadRequestMessage(IActionResult result, string expectedMessagePart)
    {
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
        Assert.Contains(expectedMessagePart, badRequestResult.Value.ToString());
    }

    private void AssertNotFoundMessage(IActionResult result, string expectedMessagePart)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        Assert.Contains(expectedMessagePart, objectResult.Value.ToString());
    }

    private void AssertForbiddenMessage(IActionResult result, string expectedMessagePart)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        Assert.Contains(expectedMessagePart, objectResult.Value.ToString());
    }
    private void AssertOkMessage(IActionResult result, string expectedMessagePart)
    {
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.Contains(expectedMessagePart, okResult.Value.ToString());
    }

    [Fact]
    public async Task CreateCarRental_ValidRequest_ReturnsOk()
    {
        SetUserContext(1, Roles.User);
        var carRentalRequest = new CarRentalRequest
        {
            CarListingId = 101,
            RentalStartDate = DateTime.Now.AddDays(5),
            RentalEndDate = DateTime.Now.AddDays(10)
        };

        var result = await _controller.CreateCarRental(carRentalRequest);

        AssertOkMessage(result, "Wypożyczenie zostało dodane.");

        var addedRental = await _context.CarRentals.FirstOrDefaultAsync(r => r.UserId == 1 && r.CarListingId == 101 && r.RentalStatus == "Aktywne");
        Assert.NotNull(addedRental);
        Assert.False(_context.CarListing.Find(101).IsAvailable);


        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Nowe wypożyczenie o ID")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task CreateCarRental_RentalEndDateBeforeStartDate_ReturnsBadRequest()
    {
        SetUserContext(1, Roles.User);
        var carRentalRequest = new CarRentalRequest
        {
            CarListingId = 101,
            RentalStartDate = DateTime.Now.AddDays(10),
            RentalEndDate = DateTime.Now.AddDays(5)
        };

        var result = await _controller.CreateCarRental(carRentalRequest);

        AssertBadRequestMessage(result, ErrorMessages.RentalEndDateBeforeStartDate);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Data zakończenia wypożyczenia")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task CreateCarRental_CarNotFound_ReturnsNotFound()
    {
        SetUserContext(1, Roles.User);
        var carRentalRequest = new CarRentalRequest
        {
            CarListingId = 999,
            RentalStartDate = DateTime.Now.AddDays(1),
            RentalEndDate = DateTime.Now.AddDays(2)
        };

        var result = await _controller.CreateCarRental(carRentalRequest);

        AssertNotFoundMessage(result, ErrorMessages.ListingNotFound);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Próba wypożyczenia nieistniejącego samochodu")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task CreateCarRental_CarNotAvailable_ReturnsBadRequest()
    {
        SetUserContext(1, Roles.User);
        var carRentalRequest = new CarRentalRequest
        {
            CarListingId = 102,
            RentalStartDate = DateTime.Now.AddDays(1),
            RentalEndDate = DateTime.Now.AddDays(2)
        };

        var result = await _controller.CreateCarRental(carRentalRequest);

        AssertBadRequestMessage(result, ErrorMessages.CarNotAvailable);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Próba wypożyczenia niedostępnego samochodu")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task CreateCarRental_CarNotApproved_ReturnsBadRequest()
    {
        SetUserContext(1, Roles.User);
        var carRentalRequest = new CarRentalRequest
        {
            CarListingId = 103,
            RentalStartDate = DateTime.Now.AddDays(1),
            RentalEndDate = DateTime.Now.AddDays(2)
        };

        var result = await _controller.CreateCarRental(carRentalRequest);

        AssertBadRequestMessage(result, ErrorMessages.CarNotApproved);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Próba wypożyczenia niezatwierdzonego samochodu")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task CreateCarRental_CannotRentOwnCar_ReturnsBadRequest()
    {
        SetUserContext(1, Roles.User);
        var carRentalRequest = new CarRentalRequest
        {
            CarListingId = 104,
            RentalStartDate = DateTime.Now.AddDays(1),
            RentalEndDate = DateTime.Now.AddDays(2)
        };

        var result = await _controller.CreateCarRental(carRentalRequest);

        AssertBadRequestMessage(result, ErrorMessages.CannotRentOwnCar);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Użytkownik")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task CreateCarRental_CarAlreadyRentedInPeriod_ReturnsBadRequest()
    {
        SetUserContext(1, Roles.User);
        var carRentalRequest = new CarRentalRequest
        {
            CarListingId = 101,
            RentalStartDate = DateTime.Now.AddDays(-1),
            RentalEndDate = DateTime.Now.AddDays(1)
        };

        var result = await _controller.CreateCarRental(carRentalRequest);

        AssertBadRequestMessage(result, ErrorMessages.CarAlreadyRentedInPeriod);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Samochód")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUserCarRentals_UserHasActiveRentals_ReturnsOkWithRentals()
    {
        SetUserContext(1, Roles.User);

        var result = await _controller.GetUserCarRentals();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rentals = Assert.IsAssignableFrom<List<CarRental>>(okResult.Value);
        Assert.Single(rentals);
        Assert.Equal(2, rentals.First().CarRentalId);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pomyślnie pobrano")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUserCarRentals_UserHasNoActiveRentals_ReturnsNotFound()
    {
        SetUserContext(2, Roles.User);

        var result = await _controller.GetUserCarRentals();

        AssertNotFoundMessage(result, ErrorMessages.NoActiveRentalsForUser);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Brak aktywnych wypożyczeń")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetAllCarRentals_AdminUser_ReturnsOkWithAllRentals()
    {
        SetUserContext(3, Roles.Admin);

        var result = await _controller.GetAllCarRentals();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rentals = Assert.IsAssignableFrom<List<CarRental>>(okResult.Value);
        Assert.Equal(3, rentals.Count);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pomyślnie pobrano")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetAllCarRentals_NoRentalsExist_ReturnsNotFound()
    {
        _context.CarRentals.RemoveRange(_context.CarRentals);
        _context.SaveChanges();
        SetUserContext(3, Roles.Admin);

        var result = await _controller.GetAllCarRentals();

        AssertNotFoundMessage(result, ErrorMessages.NoRentalsFound);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Brak wypożyczeń w systemie")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetCarRental_AsRenter_ReturnsOk()
    {
        SetUserContext(1, Roles.User);

        var result = await _controller.GetCarRental(2);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rental = Assert.IsType<CarRental>(okResult.Value);
        Assert.Equal(2, rental.CarRentalId);
        Assert.Equal(1, rental.UserId);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pomyślnie pobrano wypożyczenie")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetCarRental_AsOwner_ReturnsOk()
    {
        SetUserContext(2, Roles.User);

        var result = await _controller.GetCarRental(2);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rental = Assert.IsType<CarRental>(okResult.Value);
        Assert.Equal(2, rental.CarRentalId);
        Assert.Equal(101, rental.CarListingId);
        Assert.Equal(2, rental.CarListing.UserId);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pomyślnie pobrano wypożyczenie")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetCarRental_AsAdmin_ReturnsOk()
    {
        SetUserContext(3, Roles.Admin);

        var result = await _controller.GetCarRental(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rental = Assert.IsType<CarRental>(okResult.Value);
        Assert.Equal(1, rental.CarRentalId);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pomyślnie pobrano wypożyczenie")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetCarRental_NotFound_ReturnsNotFound()
    {
        SetUserContext(1, Roles.User);

        var result = await _controller.GetCarRental(999);

        AssertNotFoundMessage(result, ErrorMessages.RentalNotFound);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Wypożyczenie o ID:")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetCarRental_NotOwnerRenterOrAdmin_ReturnsForbidden()
    {
        SetUserContext(4, Roles.User);

        var result = await _controller.GetCarRental(1);

        AssertForbiddenMessage(result, ErrorMessages.NotOwnerRenterOrAdmin);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("próbował uzyskać dostęp do wypożyczenia")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCarRentalStatus_AsOwnerToEnded_ReturnsOkAndCarAvailable()
    {
        SetUserContext(2, Roles.User);
        var rentalIdToUpdate = 2;
        var newStatus = "Zakończone";

        var result = await _controller.UpdateCarRentalStatus(rentalIdToUpdate, newStatus);

        AssertOkMessage(result, "Status wypożyczenia został zmieniony.");

        var updatedRental = await _context.CarRentals.FindAsync(rentalIdToUpdate);
        Assert.Equal(newStatus, updatedRental.RentalStatus);
        Assert.True(_context.CarListing.Find(101).IsAvailable);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Dostępność samochodu ID:")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCarRentalStatus_AsAdminToCanceled_ReturnsOkAndCarAvailable()
    {
        SetUserContext(3, Roles.Admin);
        var rentalIdToUpdate = 2;
        var newStatus = "Anulowane";

        var result = await _controller.UpdateCarRentalStatus(rentalIdToUpdate, newStatus);

        AssertOkMessage(result, "Status wypożyczenia został zmieniony.");

        var updatedRental = await _context.CarRentals.FindAsync(rentalIdToUpdate);
        Assert.Equal(newStatus, updatedRental.RentalStatus);
        Assert.True(_context.CarListing.Find(101).IsAvailable);
    }

    [Fact]
    public async Task UpdateCarRentalStatus_NotFound_ReturnsNotFound()
    {
        SetUserContext(2, Roles.User);
        var newStatus = "Zakończone";

        var result = await _controller.UpdateCarRentalStatus(999, newStatus);

        AssertNotFoundMessage(result, ErrorMessages.RentalNotFound);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Próba zmiany statusu nieistniejącego wypożyczenia")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCarRentalStatus_NotOwnerOrAdmin_ReturnsForbidden()
    {
        SetUserContext(1, Roles.User);
        var rentalIdToUpdate = 2;
        var newStatus = "Zakończone";

        var result = await _controller.UpdateCarRentalStatus(rentalIdToUpdate, newStatus);

        AssertForbiddenMessage(result, ErrorMessages.NotOwnerOrAdminRentalStatus);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("próbował zmienić status wypożyczenia")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUserCarRentalHistory_UserHasHistory_ReturnsOkWithRentals()
    {
        SetUserContext(1, Roles.User);

        var result = await _controller.GetUserCarRentalHistory();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rentals = Assert.IsAssignableFrom<List<CarRental>>(okResult.Value);
        Assert.Equal(2, rentals.Count);
        Assert.Contains(rentals, r => r.CarRentalId == 1);
        Assert.Contains(rentals, r => r.CarRentalId == 3);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pomyślnie pobrano")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUserCarRentalHistory_UserHasNoHistory_ReturnsNotFound()
    {
        SetUserContext(2, Roles.User);

        var result = await _controller.GetUserCarRentalHistory();

        AssertNotFoundMessage(result, ErrorMessages.NoEndedRentalsForUser);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Brak zakończonych/anulowanych wypożyczeń")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task AddReview_ValidRequest_ReturnsOkAndUpdatesAverageRating()
    {
        SetUserContext(1, Roles.User);
        var rental = _context.CarRentals.Find(1);
        if (rental != null) rental.RentalStatus = "Zakończone";
        _context.SaveChanges();

        var reviewRequest = new CarRentalReviewRequest
        {
            CarRentalId = 1,
            Rating = 4,
            Comment = "Great car and service!"
        };

        var result = await _controller.AddReview(reviewRequest);

        AssertOkMessage(result, "Recenzja została dodana.");

        var addedReview = await _context.CarRentalReviews.FirstOrDefaultAsync(r => r.CarRentalId == 1 && r.UserId == 1);
        Assert.NotNull(addedReview);
        Assert.Equal(4, addedReview.Rating);

        var carListing = await _context.CarListing.FindAsync(101);
        Assert.Equal(4.00, carListing.AverageRating);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Recenzja o ID:")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Średnia ocena dla ogłoszenia ID:")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task AddReview_InvalidRating_ReturnsBadRequest()
    {
        SetUserContext(1, Roles.User);
        var rental = _context.CarRentals.Find(1);
        if (rental != null) rental.RentalStatus = "Zakończone";
        _context.SaveChanges();

        var reviewRequest = new CarRentalReviewRequest
        {
            CarRentalId = 1,
            Rating = 0,
            Comment = "Test"
        };

        var result = await _controller.AddReview(reviewRequest);

        AssertBadRequestMessage(result, ErrorMessages.InvalidRating);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Nieprawidłowa ocena")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task AddReview_RentalNotFoundForUser_ReturnsNotFound()
    {
        SetUserContext(2, Roles.User);
        var reviewRequest = new CarRentalReviewRequest
        {
            CarRentalId = 1,
            Rating = 5,
            Comment = "Test"
        };

        var result = await _controller.AddReview(reviewRequest);

        AssertNotFoundMessage(result, "Wypożyczenie nie istnieje.");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Nie znaleziono wypożyczenia")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task AddReview_RentalNotEnded_ReturnsBadRequest()
    {
        SetUserContext(1, Roles.User);
        var rental = _context.CarRentals.Find(2);
        if (rental != null) rental.RentalStatus = "Aktywne";
        _context.SaveChanges();

        var reviewRequest = new CarRentalReviewRequest
        {
            CarRentalId = 2,
            Rating = 4,
            Comment = "Review for active rental"
        };

        var result = await _controller.AddReview(reviewRequest);

        AssertBadRequestMessage(result, "Recenzja może być wystawiona tylko dla zakończonych wypożyczeń."); 

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Próba dodania recenzji dla niezakończonego wypożyczenia")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task AddReview_AlreadyReviewed_ReturnsBadRequest()
    {
        SetUserContext(1, Roles.User);
        var rental = _context.CarRentals.Find(1);
        if (rental != null) rental.RentalStatus = "Zakończone";
        _context.SaveChanges();

        _context.CarRentalReviews.Add(new CarRentalReview { CarRentalId = 1, UserId = 1, Rating = 3, Comment = "First review" });
        _context.SaveChanges();

        var reviewRequest = new CarRentalReviewRequest
        {
            CarRentalId = 1,
            Rating = 5,
            Comment = "Second review"
        };

        var result = await _controller.AddReview(reviewRequest);

        AssertBadRequestMessage(result, ErrorMessages.AlreadyReviewed);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("próbował dodać drugą recenzję")), 
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}