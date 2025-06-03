using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FrogCar.Controllers;
using FrogCar.Data;
using FrogCar.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using FrogCar.Constants;
using System.Text.Json;

namespace FrogCar.Tests.Controllers;
public class AdminControllerTests
{
    private readonly AppDbContext _context;
    private readonly Mock<IPasswordValidator> _mockPasswordValidator;
    private readonly Mock<ILogger<AdminController>> _mockLogger;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockPasswordValidator = new Mock<IPasswordValidator>();
        _mockLogger = new Mock<ILogger<AdminController>>();

        _controller = new AdminController(
            _context,
            _mockPasswordValidator.Object,
            _mockLogger.Object);
    }

    private void ClearDatabase()
    {
        _context.Users.RemoveRange(_context.Users);
        _context.CarListing.RemoveRange(_context.CarListing);
        _context.CarRentals.RemoveRange(_context.CarRentals);
        _context.CarRentalReviews.RemoveRange(_context.CarRentalReviews);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetUsers_ReturnsOkResultWithUsers()
    {
        ClearDatabase();
        _context.Users.AddRange(
            new User { Id = 1, Username = "user1", Email = "user1@example.com", Role = Roles.User, Password = "hashedpassword1" },
            new User { Id = 2, Username = "admin1", Email = "admin1@example.com", Role = Roles.Admin, Password = "hashedpassword2" }
        );
        await _context.SaveChangesAsync();

        var result = await _controller.GetUsers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value).ToList();
        Assert.Equal(2, users.Count);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbuje pobrać listę użytkowników.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUsers_NoUsers_ReturnsOkResultWithEmptyList()
    {
        ClearDatabase();

        var result = await _controller.GetUsers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value).ToList();
        Assert.Empty(users);
    }

    [Fact]
    public async Task GetUserById_UserExists_ReturnsOkResultWithUser()
    {
        ClearDatabase();
        var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Role = Roles.User, Password = "hashedpassword" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _controller.GetUserById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);

        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(okResult.Value));

        Assert.NotNull(jsonElement);
        Assert.Equal(1, jsonElement.GetProperty("Id").GetInt32());
        Assert.Equal("testuser", jsonElement.GetProperty("Username").GetString());
        Assert.Equal("test@example.com", jsonElement.GetProperty("Email").GetString());


        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbuje pobrać użytkownika o ID: 1")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUserById_UserDoesNotExist_ReturnsNotFoundResult()
    {
        ClearDatabase();

        var result = await _controller.GetUserById(99);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var message = notFoundResult.Value.GetType().GetProperty("message").GetValue(notFoundResult.Value);
        Assert.Equal(ErrorMessages.UserNotFound, message);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbował pobrać nieistniejącego użytkownika o ID: 99")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetAllListings_ReturnsOkResultWithListings()
    {
        ClearDatabase();
        var user = new User { Id = 1, Username = "owner", Email = "owner@example.com", Role = Roles.User, Password = "hashedpassword" };
        _context.Users.Add(user);
        _context.CarListing.AddRange(
            new CarListing { Id = 1, UserId = user.Id, User = user, Brand = "BMW", RentalPricePerDay = 100, IsAvailable = true, IsApproved = true },
            new CarListing { Id = 2, UserId = user.Id, User = user, Brand = "Audi", RentalPricePerDay = 80, IsAvailable = true, IsApproved = false } 
        );
        await _context.SaveChangesAsync();

        var result = await _controller.GetAllListings();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var listings = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
        Assert.Equal(2, listings.Count);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbuje pobrać listę ogłoszeń.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetAllListings_NoListings_ReturnsOkResultWithEmptyList()
    {
        ClearDatabase();

        var result = await _controller.GetAllListings();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var listings = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
        Assert.Empty(listings);
    }

    [Fact]
    public async Task GetAllRentals_ReturnsOkResultWithRentals()
    {
        ClearDatabase();
        var user = new User { Id = 1, Username = "renter", Email = "renter@example.com", Role = Roles.User, Password = "hashedpassword" };
        var owner = new User { Id = 2, Username = "owner", Email = "owner@example.com", Role = Roles.User, Password = "hashedpassword" };
        var listing = new CarListing { Id = 1, UserId = owner.Id, User = owner, Brand = "BMW", RentalPricePerDay = 100, IsAvailable = true, IsApproved = true }; 
        _context.Users.AddRange(user, owner);
        _context.CarListing.Add(listing);
        _context.CarRentals.AddRange(
            new CarRental { CarRentalId = 1, UserId = user.Id, User = user, CarListingId = listing.Id, CarListing = listing, RentalPrice = 200, RentalStatus = "Zakończone" }, 
            new CarRental { CarRentalId = 2, UserId = user.Id, User = user, CarListingId = listing.Id, CarListing = listing, RentalPrice = 150, RentalStatus = "Aktywne" }     
        );
        await _context.SaveChangesAsync();

        var result = await _controller.GetAllRentals();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rentals = Assert.IsAssignableFrom<List<CarRental>>(okResult.Value);
        Assert.Equal(2, rentals.Count);
        Assert.NotNull(rentals.First().User);
        Assert.NotNull(rentals.First().CarListing);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbuje pobrać listę wypożyczeń.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetAllRentals_NoRentals_ReturnsOkResultWithEmptyList()
    {
        ClearDatabase();
 
        var result = await _controller.GetAllRentals();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rentals = Assert.IsAssignableFrom<List<CarRental>>(okResult.Value);
        Assert.Empty(rentals);
    }

    [Fact]
    public async Task GetAllReviews_ReturnsOkResultWithReviews()
    {
        ClearDatabase();
        var user = new User { Id = 1, Username = "reviewer", Email = "reviewer@example.com", Role = Roles.User, Password = "hashedpassword" };
        var owner = new User { Id = 2, Username = "owner", Email = "owner@example.com", Role = Roles.User, Password = "hashedpassword" };
        var listing = new CarListing { Id = 1, UserId = owner.Id, User = owner, Brand = "BMW", RentalPricePerDay = 100, IsAvailable = true, IsApproved = true }; 
        var rental = new CarRental { CarRentalId = 1, UserId = user.Id, User = user, CarListingId = listing.Id, CarListing = listing, RentalPrice = 200, RentalStatus = "Zakończone" }; 
        _context.Users.AddRange(user, owner);
        _context.CarListing.Add(listing);
        _context.CarRentals.Add(rental);
        _context.CarRentalReviews.AddRange(
            new CarRentalReview { ReviewId = 1, CarRentalId = rental.CarRentalId, CarRental = rental, UserId = user.Id, User = user, Rating = 5, Comment = "Great car!" },
            new CarRentalReview { ReviewId = 2, CarRentalId = rental.CarRentalId, CarRental = rental, UserId = user.Id, User = user, Rating = 4, Comment = "Good experience." }
        );
        await _context.SaveChangesAsync();

        var result = await _controller.GetAllReviews();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var reviews = Assert.IsAssignableFrom<List<CarRentalReview>>(okResult.Value);
        Assert.Equal(2, reviews.Count);
        Assert.NotNull(reviews.First().User);
        Assert.NotNull(reviews.First().CarRental);
        Assert.NotNull(reviews.First().CarRental.CarListing); 
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbuje pobrać listę recenzji.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetAllReviews_NoReviews_ReturnsOkResultWithEmptyList()
    {
        ClearDatabase();

        var result = await _controller.GetAllReviews();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var reviews = Assert.IsAssignableFrom<List<CarRentalReview>>(okResult.Value);
        Assert.Empty(reviews);
    }

    [Fact]
    public async Task DeleteUser_UserExists_ReturnsOkResult()
    {
        ClearDatabase();
        var user = new User { Id = 1, Username = "userToDelete", Email = "delete@example.com", Role = Roles.User, Password = "hashedpassword" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteUser(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Użytkownik został usunięty.", okResult.Value);
        Assert.Null(await _context.Users.FindAsync(1));
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbuje usunąć użytkownika o ID: 1")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Użytkownik o ID: 1 został usunięty przez admina.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task DeleteUser_UserDoesNotExist_ReturnsNotFoundResult()
    {
        ClearDatabase();

        var result = await _controller.DeleteUser(99);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var message = notFoundResult.Value.GetType().GetProperty("message").GetValue(notFoundResult.Value);
        Assert.Equal(ErrorMessages.UserNotFound, message);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbował usunąć nieistniejącego użytkownika o ID: 99")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task DeleteReview_ReviewExists_ReturnsOkResultAndUpdatesListingRating()
    {
        ClearDatabase();
        var user = new User { Id = 1, Username = "reviewer", Email = "reviewer@example.com", Role = Roles.User, Password = "hashedpassword" };
        var owner = new User { Id = 2, Username = "owner", Email = "owner@example.com", Role = Roles.User, Password = "hashedpassword" };
        var listing = new CarListing { Id = 10, UserId = owner.Id, User = owner, Brand = "BMW", RentalPricePerDay = 100, AverageRating = 4.0 };
        var rental = new CarRental { CarRentalId = 100, UserId = user.Id, User = user, CarListingId = listing.Id, CarListing = listing, RentalPrice = 200, RentalStatus = "Zakończone" }; 
        var reviewToDelete = new CarRentalReview { ReviewId = 1, CarRentalId = rental.CarRentalId, CarRental = rental, UserId = user.Id, User = user, Rating = 5, Comment = "Excellent!" };
        var anotherReview = new CarRentalReview { ReviewId = 2, CarRentalId = rental.CarRentalId, CarRental = rental, UserId = user.Id, User = user, Rating = 3, Comment = "OK." };

        _context.Users.AddRange(user, owner);
        _context.CarListing.Add(listing);
        _context.CarRentals.Add(rental);
        _context.CarRentalReviews.AddRange(reviewToDelete, anotherReview);
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteReview(reviewToDelete.ReviewId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Recenzja została usunięta.", okResult.Value);
        Assert.Null(await _context.CarRentalReviews.FindAsync(reviewToDelete.ReviewId));

        var updatedListing = await _context.CarListing.FindAsync(listing.Id); 
        Assert.Equal(3.00, updatedListing.AverageRating); 
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Recenzja o ID: {reviewToDelete.ReviewId} została usunięta przez admina.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Zaktualizowano średnią ocenę dla ogłoszenia ID: {listing.Id} na 3")), 
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task DeleteReview_ReviewDoesNotExist_ReturnsNotFoundResult()
    {
        ClearDatabase();

        var result = await _controller.DeleteReview(99);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var message = notFoundResult.Value.GetType().GetProperty("message").GetValue(notFoundResult.Value);
        Assert.Equal("Recenzja nie istnieje.", message);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbował usunąć nieistniejącą recenzję o ID: 99")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUser_UserExistsAndValidModel_ReturnsOkResult()
    {
        ClearDatabase();
        var user = new User { Id = 1, Username = "olduser", Email = "old@example.com", Role = Roles.User, Password = "oldhashedpassword" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var updateModel = new UpdateUserModel { Username = "newuser", Email = "new@example.com", Password = "NewPassword1!", Role = Roles.Admin };
        _mockPasswordValidator.Setup(p => p.Validate(updateModel.Password)).Returns((string)null);
        _mockPasswordValidator.Setup(p => p.Hash(updateModel.Password)).Returns("newhashedpassword");

        var result = await _controller.UpdateUser(1, updateModel);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Użytkownik został zaktualizowany.", okResult.Value);
        var updatedUser = await _context.Users.FindAsync(1);
        Assert.Equal("newuser", updatedUser.Username);
        Assert.Equal("new@example.com", updatedUser.Email);
        Assert.Equal("newhashedpassword", updatedUser.Password);
        Assert.Equal(Roles.Admin, updatedUser.Role);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbuje zaktualizować użytkownika o ID: 1")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Użytkownik o ID: 1 został zaktualizowany przez admina.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUser_UserDoesNotExist_ReturnsNotFoundResult()
    {

        ClearDatabase();
        var updateModel = new UpdateUserModel { Username = "newuser" };

        var result = await _controller.UpdateUser(99, updateModel);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var message = notFoundResult.Value.GetType().GetProperty("message").GetValue(notFoundResult.Value);
        Assert.Equal(ErrorMessages.UserNotFound, message);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbował zaktualizować nieistniejącego użytkownika o ID: 99")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUser_UsernameTaken_ReturnsBadRequest()
    {
        ClearDatabase();
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@example.com", Role = Roles.User, Password = "hashedpassword" };
        var user2 = new User { Id = 2, Username = "existinguser", Email = "user2@example.com", Role = Roles.User, Password = "hashedpassword" };
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var updateModel = new UpdateUserModel { Username = "existinguser" };

        var result = await _controller.UpdateUser(1, updateModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value);
        Assert.Equal(ErrorMessages.UsernameTaken, message);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbował zmienić nazwę użytkownika na już istniejącą: existinguser")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUser_EmailTaken_ReturnsBadRequest()
    {
        ClearDatabase();
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@example.com", Role = Roles.User, Password = "hashedpassword" };
        var user2 = new User { Id = 2, Username = "user2", Email = "existing@example.com", Role = Roles.User, Password = "hashedpassword" };
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var updateModel = new UpdateUserModel { Email = "existing@example.com" };

        var result = await _controller.UpdateUser(1, updateModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value);
        Assert.Equal(ErrorMessages.EmailTaken, message);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbował zmienić email użytkownika na już istniejący: existing@example.com")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUser_InvalidPasswordFormat_ReturnsBadRequest()
    {
        ClearDatabase();
        var user = new User { Id = 1, Username = "user1", Email = "user1@example.com", Role = Roles.User, Password = "hashedpassword" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var updateModel = new UpdateUserModel { Password = "weak" };
        _mockPasswordValidator.Setup(p => p.Validate(updateModel.Password)).Returns("Hasło jest za krótkie.");

        var result = await _controller.UpdateUser(1, updateModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value);
        Assert.Equal("Hasło jest za krótkie.", message);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Błąd walidacji hasła podczas aktualizacji użytkownika 1: Hasło jest za krótkie.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUser_InvalidRole_ReturnsBadRequest()
    {
        ClearDatabase();
        var user = new User { Id = 1, Username = "user1", Email = "user1@example.com", Role = Roles.User, Password = "hashedpassword" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var updateModel = new UpdateUserModel { Role = "InvalidRole" };

        var result = await _controller.UpdateUser(1, updateModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value);
        Assert.Equal("Niepoprawna rola. Dozwolone role: User, Admin.", message);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbował ustawić niepoprawną rolę dla użytkownika 1: InvalidRole")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUser_PartialUpdate_UpdatesOnlySpecifiedFields()
    {
        ClearDatabase();
        var user = new User { Id = 1, Username = "originaluser", Email = "original@example.com", Role = Roles.User, Password = "originalhashedpassword" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var updateModel = new UpdateUserModel { Email = "updated@example.com" }; 
        _mockPasswordValidator.Setup(p => p.Validate(It.IsAny<string>())).Returns((string)null); 

        var result = await _controller.UpdateUser(1, updateModel);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Użytkownik został zaktualizowany.", okResult.Value);
        var updatedUser = await _context.Users.FindAsync(1);
        Assert.Equal("originaluser", updatedUser.Username); 
        Assert.Equal("updated@example.com", updatedUser.Email); 
        Assert.Equal("originalhashedpassword", updatedUser.Password); 
        Assert.Equal(Roles.User, updatedUser.Role); 
    }

    [Fact]
    public async Task GetAdminStats_ReturnsOkResultWithCorrectStats()
    {
        ClearDatabase();
        var user1 = new User { Id = 1, Username = "u1", Email = "u1@e.com", Role = Roles.User, Password = "p" };
        var user2 = new User { Id = 2, Username = "u2", Email = "u2@e.com", Role = Roles.Admin, Password = "p" };
        var listing1 = new CarListing { Id = 1, UserId = 1, Brand = "BMW", RentalPricePerDay = 100, IsAvailable = true, IsApproved = true };
        var listing2 = new CarListing { Id = 2, UserId = 1, Brand = "Audi", RentalPricePerDay = 80, IsAvailable = false, IsApproved = true };
        var listing3 = new CarListing { Id = 3, UserId = 2, Brand = "Mercedes", RentalPricePerDay = 120, IsAvailable = true, IsApproved = false }; 
        var rental1 = new CarRental { CarRentalId = 1, CarListingId = 1, UserId = 1, RentalStatus = "Zakończone" };
        var rental2 = new CarRental { CarRentalId = 2, CarListingId = 1, UserId = 1, RentalStatus = "W toku" };
        var rental3 = new CarRental { CarRentalId = 3, CarListingId = 2, UserId = 2, RentalStatus = "Zakończone" };
        var review1 = new CarRentalReview { ReviewId = 1, CarRentalId = 1, UserId = 1, Rating = 5 };
        var review2 = new CarRentalReview { ReviewId = 2, CarRentalId = 3, UserId = 2, Rating = 3 };

        _context.Users.AddRange(user1, user2);
        _context.CarListing.AddRange(listing1, listing2, listing3);
        _context.CarRentals.AddRange(rental1, rental2, rental3);
        _context.CarRentalReviews.AddRange(review1, review2);
        await _context.SaveChangesAsync();

        var result = await _controller.GetAdminStats();

        var okResult = Assert.IsType<OkObjectResult>(result);

        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(okResult.Value));

        Assert.NotNull(jsonElement);
        Assert.Equal(2, jsonElement.GetProperty("TotalUsers").GetInt32());
        Assert.Equal(3, jsonElement.GetProperty("TotalListings").GetInt32());
        Assert.Equal(1, jsonElement.GetProperty("ActiveListings").GetInt32());
        Assert.Equal(3, jsonElement.GetProperty("TotalRentals").GetInt32());
        Assert.Equal(2, jsonElement.GetProperty("EndedRentals").GetInt32());
        Assert.Equal(2, jsonElement.GetProperty("TotalReviews").GetInt32());
        Assert.Equal(4.00, jsonElement.GetProperty("AverageRating").GetDouble());

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin próbuje pobrać statystyki ogólne.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Admin pobrał statystyki ogólne.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetAdminStats_NoData_ReturnsOkResultWithZeroStats()
    {
        ClearDatabase();
        var result = await _controller.GetAdminStats();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(okResult.Value));

        Assert.NotNull(jsonElement);
        Assert.Equal(0, jsonElement.GetProperty("TotalUsers").GetInt32());
        Assert.Equal(0, jsonElement.GetProperty("TotalListings").GetInt32());
        Assert.Equal(0, jsonElement.GetProperty("ActiveListings").GetInt32());
        Assert.Equal(0, jsonElement.GetProperty("TotalRentals").GetInt32());
        Assert.Equal(0, jsonElement.GetProperty("EndedRentals").GetInt32());
        Assert.Equal(0, jsonElement.GetProperty("TotalReviews").GetInt32());
        Assert.Equal(0.00, jsonElement.GetProperty("AverageRating").GetDouble());
    }
}