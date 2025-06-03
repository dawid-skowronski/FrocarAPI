using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FrogCar.Controllers;
using FrogCar.Data;
using FrogCar.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using FrogCar.Constants;
using System.Net.Mail;

namespace FrogCar.Tests.Controllers;
public class AccountControllerTests
{
    private readonly AppDbContext _context;
    private readonly Mock<IPasswordValidator> _mockPasswordValidator;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<ILogger<AccountController>> _mockLogger;
    private readonly AccountController _controller;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<EmailService> _mockEmailService;

    public AccountControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockPasswordValidator = new Mock<IPasswordValidator>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockLogger = new Mock<ILogger<AccountController>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockEmailService = new Mock<EmailService>(_mockConfiguration.Object);

        _mockConfiguration.Setup(c => c["Email:TestMode"]).Returns("true");
        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("supersecretkeythatisatleast32byteslong"); 
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c["Jwt:EmailSecretKey"]).Returns("supersecretemailkeyatleast32byteslong"); 
        _mockConfiguration.Setup(c => c["Frontend:ResetPasswordUrl"]).Returns("http://localhost:3000/reset-password");


        _controller = new AccountController(
            _context,
            _mockPasswordValidator.Object,
            _mockJwtTokenService.Object,
            _mockEmailService.Object, 
            _mockLogger.Object);
    }

    private void ClearDatabase()
    {
        _context.Users.RemoveRange(_context.Users);
        _context.Notifications.RemoveRange(_context.Notifications);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        ClearDatabase();
        var loginModel = new LoginModel { Username = "testuser", Password = "wrongpassword" };
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = "User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockPasswordValidator.Setup(p => p.Verify(loginModel.Password, user.Password)).Returns(false);

        var result = await _controller.Login(loginModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.InvalidCredentials, badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }

    [Fact]
    public async Task Login_TokenGenerationFails_ReturnsInternalServerError()
    {
        ClearDatabase();
        var loginModel = new LoginModel { Username = "testuser", Password = "password" };
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = "User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockPasswordValidator.Setup(p => p.Verify(loginModel.Password, user.Password)).Returns(true);
        _mockJwtTokenService.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("");

        var result = await _controller.Login(loginModel);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        Assert.Equal("Błąd generowania tokenu.", statusCodeResult.Value.GetType().GetProperty("message").GetValue(statusCodeResult.Value));
    }

    [Fact]
    public async Task Register_ValidModel_ReturnsOkResult()
    {
        ClearDatabase();
        var registerModel = new RegisterModel
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password1!"
        };

        _mockPasswordValidator.Setup(p => p.Validate(registerModel.Password)).Returns((string)null);
        _mockPasswordValidator.Setup(p => p.Hash(registerModel.Password)).Returns("hashedpassword");

        var result = await _controller.Register(registerModel);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Rejestracja zakończona sukcesem", okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        Assert.NotNull(await _context.Users.FirstOrDefaultAsync(u => u.Username == "newuser"));
    }

    [Fact]
    public async Task Register_InvalidModel_ReturnsBadRequest()
    {
        ClearDatabase();
        var registerModel = new RegisterModel
        {
            Username = "", 
            Email = "invalid",
            Password = "short"
        };

        _controller.ModelState.AddModelError("Username", "Username is required.");
        _controller.ModelState.AddModelError("Email", "Invalid email format.");
        _controller.ModelState.AddModelError("Password", "Password is too short.");

        var result = await _controller.Register(registerModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.InvalidModel, badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        var errors = Assert.IsType<List<string>>(badRequestResult.Value.GetType().GetProperty("errors").GetValue(badRequestResult.Value));
        Assert.Contains("Username is required.", errors);
        Assert.Contains("Invalid email format.", errors);
        Assert.Contains("Password is too short.", errors);
    }

    [Fact]
    public async Task Register_InvalidPasswordFormat_ReturnsBadRequest()
    {
        ClearDatabase();
        var registerModel = new RegisterModel
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "short"
        };

        _mockPasswordValidator.Setup(p => p.Validate(registerModel.Password)).Returns("Hasło musi mieć co najmniej 8 znaków.");

        var result = await _controller.Register(registerModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Hasło musi mieć co najmniej 8 znaków.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }


    [Fact]
    public async Task Register_UsernameTaken_ReturnsBadRequest()
    {
        ClearDatabase();
        var existingUser = new User
        {
            Id = 1,
            Username = "existinguser",
            Email = "existing@example.com",
            Password = "hashedpassword",
            Role = "User"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registerModel = new RegisterModel
        {
            Username = "existinguser",
            Email = "newuser@example.com",
            Password = "Password1!"
        };

        _mockPasswordValidator.Setup(p => p.Validate(registerModel.Password)).Returns((string)null);

        var result = await _controller.Register(registerModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.UsernameTaken, badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }

    [Fact]
    public async Task Register_EmailTaken_ReturnsBadRequest()
    {
        ClearDatabase();
        var existingUser = new User
        {
            Id = 1,
            Username = "existinguser",
            Email = "existing@example.com",
            Password = "hashedpassword",
            Role = "User"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registerModel = new RegisterModel
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "Password1!"
        };

        _mockPasswordValidator.Setup(p => p.Validate(registerModel.Password)).Returns((string)null);

        var result = await _controller.Register(registerModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.EmailTaken, badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }

    [Fact]
    public async Task DeleteUser_ValidUser_ReturnsOkResult()
    {
        ClearDatabase();
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = "User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.DeleteUser();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Twoje konto zostało usunięte.", okResult.Value);
        Assert.Null(await _context.Users.FindAsync(userId));
    }

    [Fact]
    public async Task DeleteUser_UserNotFound_ReturnsNotFound()
    {
        ClearDatabase();
        var userId = 99;
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.DeleteUser();

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
        Assert.Equal(ErrorMessages.UserNotFound, statusCodeResult.Value.GetType().GetProperty("message").GetValue(statusCodeResult.Value));
    }

    [Fact]
    public async Task GetMyNotifications_ValidUser_ReturnsOkResult()
    {
        ClearDatabase();
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = "User"
        };

        var notifications = new List<Notification>
        {
            new Notification
            {
                NotificationId = 1,
                UserId = userId,
                IsRead = false,
                CreatedAt = DateTime.Now,
                Message = "Notification 1"
            },
            new Notification
            {
                NotificationId = 2,
                UserId = userId,
                IsRead = false,
                CreatedAt = DateTime.Now,
                Message = "Notification 2"
            }
        };

        _context.Users.Add(user);
        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.GetMyNotifications();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnNotifications = Assert.IsType<List<Notification>>(okResult.Value);
        Assert.Equal(2, returnNotifications.Count);
    }

    [Fact]
    public async Task GetMyNotifications_NoNotifications_ReturnsOkResultWithNoNotificationsMessage()
    {
        ClearDatabase();
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.GetMyNotifications();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(ErrorMessages.NoNotifications, okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
    }

    [Fact]
    public async Task GetMyNotifications_Unauthorized_ReturnsUnauthorized()
    {
        ClearDatabase();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.GetMyNotifications();

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(ErrorMessages.Unauthorized, unauthorizedResult.Value.GetType().GetProperty("message").GetValue(unauthorizedResult.Value));
    }

    [Fact]
    public async Task ChangeUsername_ValidModel_ReturnsOkResult()
    {
        ClearDatabase();
        var userId = 1;
        var changeUsernameModel = new ChangeUsernameModel { NewUsername = "newusername" };

        var user = new User
        {
            Id = userId,
            Username = "oldusername",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = "User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.ChangeUsername(changeUsernameModel);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Nazwa użytkownika została zmieniona.", okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        var updatedUser = await _context.Users.FindAsync(userId);
        Assert.Equal("newusername", updatedUser.Username);
    }

    [Fact]
    public async Task ChangeUsername_UsernameTaken_ReturnsBadRequest()
    {
        ClearDatabase();
        var userId = 1;
        var changeUsernameModel = new ChangeUsernameModel { NewUsername = "existingusername" };

        var user1 = new User
        {
            Id = userId,
            Username = "currentusername",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = "User"
        };
        var user2 = new User
        {
            Id = 2,
            Username = "existingusername",
            Email = "existing@example.com",
            Password = "hashedpassword",
            Role = "User"
        };

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.ChangeUsername(changeUsernameModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.UsernameTaken, badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }

    [Fact]
    public async Task ChangeUsername_EmptyUsername_ReturnsBadRequest()
    {
        ClearDatabase();
        var userId = 1;
        var changeUsernameModel = new ChangeUsernameModel { NewUsername = "" };

        var user = new User
        {
            Id = userId,
            Username = "oldusername",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = "User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.ChangeUsername(changeUsernameModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.EmptyUsername, badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }

    [Fact]
    public async Task ChangeUsername_Unauthorized_ReturnsUnauthorized()
    {
        ClearDatabase();
        var changeUsernameModel = new ChangeUsernameModel { NewUsername = "newusername" };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.ChangeUsername(changeUsernameModel);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(ErrorMessages.Unauthorized, unauthorizedResult.Value.GetType().GetProperty("message").GetValue(unauthorizedResult.Value));
    }

    [Fact]
    public async Task ChangeUsername_UserNotFound_ReturnsNotFound()
    {
        ClearDatabase();
        var userId = 999; 
        var changeUsernameModel = new ChangeUsernameModel { NewUsername = "newusername" };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.ChangeUsername(changeUsernameModel);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
        Assert.Equal(ErrorMessages.UserNotFound, statusCodeResult.Value.GetType().GetProperty("message").GetValue(statusCodeResult.Value));
    }

    [Fact]
    public async Task MarkAsRead_ValidNotification_ReturnsOkResult()
    {
        ClearDatabase();
        var userId = 1;
        var notificationId = 101;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = "User"
        };
        var notification = new Notification
        {
            NotificationId = notificationId,
            UserId = userId,
            IsRead = false,
            CreatedAt = DateTime.Now,
            Message = "Test Notification"
        };

        _context.Users.Add(user);
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.MarkAsRead(notificationId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Powiadomienie zostało oznaczone jako przeczytane.", okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        var updatedNotification = await _context.Notifications.FindAsync(notificationId);
        Assert.True(updatedNotification.IsRead);
    }

    [Fact]
    public async Task MarkAsRead_NotificationNotFound_ReturnsNotFound()
    {
        ClearDatabase();
        var userId = 1;
        var notificationId = 999; 
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.MarkAsRead(notificationId);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
        Assert.Equal("Powiadomienie nie zostało znalezione.", statusCodeResult.Value.GetType().GetProperty("message").GetValue(statusCodeResult.Value));
    }

    [Fact]
    public async Task MarkAsRead_Unauthorized_ReturnsUnauthorized()
    {
        ClearDatabase();
        var notificationId = 1;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.MarkAsRead(notificationId);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(ErrorMessages.Unauthorized, unauthorizedResult.Value.GetType().GetProperty("message").GetValue(unauthorizedResult.Value));
    }

    [Fact]
    public async Task MarkAsRead_NotificationDoesNotBelongToUser_ReturnsNotFound()
    {
        ClearDatabase();
        var userId = 1;
        var otherUserId = 2;
        var notificationId = 101;

        var user = new User { Id = userId, Username = "testuser", Email = "test@example.com", Password = "hashedpassword", Role = "User" };
        var otherUser = new User { Id = otherUserId, Username = "otheruser", Email = "other@example.com", Password = "hashedpassword", Role = "User" };

        var notification = new Notification
        {
            NotificationId = notificationId,
            UserId = otherUserId, 
            IsRead = false,
            CreatedAt = DateTime.Now,
            Message = "Test Notification"
        };

        _context.Users.AddRange(user, otherUser);
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.MarkAsRead(notificationId);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
        Assert.Equal("Powiadomienie nie zostało znalezione.", statusCodeResult.Value.GetType().GetProperty("message").GetValue(statusCodeResult.Value));
        var originalNotification = await _context.Notifications.FindAsync(notificationId);
        Assert.False(originalNotification.IsRead);
    }

    [Fact]
    public async Task RequestPasswordReset_EmptyEmail_ReturnsBadRequest()
    {
        ClearDatabase();
        var result = await _controller.RequestPasswordReset("");

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Adres email jest wymagany.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }

    [Fact]
    public async Task ResetPassword_ValidTokenAndNewPassword_ReturnsOkResult()
    {
        ClearDatabase();
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Password = "oldhashedpassword",
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetModel = new ResetPasswordModel { Token = "validresettoken", NewPassword = "NewPassword1!" };

        _mockJwtTokenService.Setup(j => j.ValidatePasswordResetToken(resetModel.Token)).Returns(userId);
        _mockPasswordValidator.Setup(p => p.Validate(resetModel.NewPassword)).Returns((string)null);
        _mockPasswordValidator.Setup(p => p.Hash(resetModel.NewPassword)).Returns("newhashedpassword");

        var result = await _controller.ResetPassword(resetModel);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Hasło zostało zresetowane pomyślnie.", okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        var updatedUser = await _context.Users.FindAsync(userId);
        Assert.Equal("newhashedpassword", updatedUser.Password);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        ClearDatabase();
        var resetModel = new ResetPasswordModel { Token = "invalidtoken", NewPassword = "NewPassword1!" };

        _mockJwtTokenService.Setup(j => j.ValidatePasswordResetToken(resetModel.Token))
            .Throws(new SecurityTokenException("Nieprawidłowy lub wygasły token resetujący."));

        var result = await _controller.ResetPassword(resetModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.InvalidToken, badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }

    [Fact]
    public async Task ResetPassword_InvalidNewPassword_ReturnsBadRequest()
    {
        ClearDatabase();
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Password = "oldhashedpassword",
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetModel = new ResetPasswordModel { Token = "validresettoken", NewPassword = "weak" };

        _mockJwtTokenService.Setup(j => j.ValidatePasswordResetToken(resetModel.Token)).Returns(userId);
        _mockPasswordValidator.Setup(p => p.Validate(resetModel.NewPassword)).Returns("Hasło jest za krótkie.");

        var result = await _controller.ResetPassword(resetModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Hasło jest za krótkie.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }

    [Fact]
    public async Task ResetPassword_UserNotFoundForToken_ReturnsBadRequest()
    {
        ClearDatabase();
        var userId = 999;
        var resetModel = new ResetPasswordModel { Token = "validtokenfornonexistentuser", NewPassword = "NewPassword1!" };

        _mockJwtTokenService.Setup(j => j.ValidatePasswordResetToken(resetModel.Token)).Returns(userId);
        _mockPasswordValidator.Setup(p => p.Validate(resetModel.NewPassword)).Returns((string)null);

        var result = await _controller.ResetPassword(resetModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.InvalidToken, badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }

    [Fact]
    public async Task ResetPassword_InternalServerError_ReturnsInternalServerError()
    {
        ClearDatabase();
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Password = "oldhashedpassword",
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetModel = new ResetPasswordModel { Token = "validresettoken", NewPassword = "NewPassword1!" };

        _mockJwtTokenService.Setup(j => j.ValidatePasswordResetToken(resetModel.Token)).Returns(userId);
        _mockPasswordValidator.Setup(p => p.Validate(resetModel.NewPassword)).Returns((string)null);
        _mockPasswordValidator.Setup(p => p.Hash(resetModel.NewPassword)).Throws(new Exception("Błąd podczas hashowania hasła."));

        var result = await _controller.ResetPassword(resetModel);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        Assert.Equal("Wystąpił błąd podczas resetowania hasła.", statusCodeResult.Value.GetType().GetProperty("message").GetValue(statusCodeResult.Value));
    }

    [Fact]
    public async Task Login_InvalidModel_ReturnsBadRequest()
    {
        ClearDatabase();
        var loginModel = new LoginModel { Username = "", Password = "" }; 

        var result = await _controller.Login(loginModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.InvalidLoginData, badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }

    [Fact]
    public async Task Login_UserDoesNotExist_ReturnsBadRequest()
    {
        ClearDatabase();
        var loginModel = new LoginModel { Username = "nonexistentuser", Password = "password" };

        var result = await _controller.Login(loginModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.InvalidCredentials, badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
    }
}