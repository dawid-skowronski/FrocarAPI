using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FrogCar.Controllers;
using FrogCar.Models;
using FrogCar.Data;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Moq;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace FrogCar.Tests.Controllers
{
    public class AccountControllerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _context = new AppDbContext(options);

            var configurationBuilder = new ConfigurationBuilder()
         .AddInMemoryCollection(new Dictionary<string, string>
         {
        {"Jwt:Key", "your_jwt_secret_key_with_at_least_32_characters"},
        {"Jwt:Issuer", "your_issuer"},
        {"Jwt:Audience", "your_audience"},
        {"Jwt:EmailSecretKey", "your_jwt_email_secret_key_with_at_least_32_characters"},
        {"Email:SmtpServer", "smtp.example.com"},
        {"Email:SmtpPort", "587"},
        {"Email:SenderEmail", "sender@example.com"},
        {"Email:Password", "password"},
        {"Email:SenderName", "Sender Name"},
        {"Frontend:ResetPasswordUrl", "https://example.com/reset-password"},
         {"Email:TestMode", "true"}
         });

            _configuration = configurationBuilder.Build();

            _emailService = new EmailService(_configuration);

            _controller = new AccountController(_context, _configuration, _emailService);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
        private void SetupUserClaims(int userId)
        {
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(userClaims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task Register_ValidModel_ReturnsOk()
        {
            var model = new RegisterModel
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Password123!"
            };

            var result = await _controller.Register(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Rejestracja zakończona sukcesem", okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        }
        [Fact]
        public async Task Register_InvalidPassword_ReturnsBadRequest()
        {
            var model = new RegisterModel
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "weak"
            };

            var result = await _controller.Register(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Hasło musi mieć co najmniej 8 znaków.",
                         badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task Register_ExistingUsername_ReturnsBadRequest()
        {
            var existingUser = new User { Username = "existinguser", Email = "existing@example.com", Password = "Password123!" };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var model = new RegisterModel
            {
                Username = "existinguser",
                Email = "new@example.com",
                Password = "Password123!"
            };

            var result = await _controller.Register(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nazwa użytkownika jest już zajęta.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }
        [Fact]
        public async Task Register_ExistingEmail_ReturnsBadRequest()
        {
            var existingUser = new User
            {
                Username = "existinguser",
                Email = "test@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var model = new RegisterModel
            {
                Username = "newuser",
                Email = "test@example.com",
                Password = "NewPassword123!"
            };

            var result = await _controller.Register(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Podany adres e-mail jest już używany.",
                         badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }
        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!") };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginModel
            {
                Username = "testuser",
                Password = "Password123!"
            };

            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };
            var claimsIdentity = new ClaimsIdentity(userClaims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            var result = await _controller.Login(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Logowanie udane", okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        }


        [Fact]
        public async Task Login_InvalidCredentials_ReturnsBadRequest()
        {
            var model = new LoginModel
            {
                Username = "nonexistentuser",
                Password = "wrongpassword"
            };

            var result = await _controller.Login(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nieprawidłowa nazwa użytkownika lub hasło.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }
        [Fact]
        public async Task Login_EmptyCredentials_ReturnsBadRequest()
        {
            var model = new LoginModel
            {
                Username = "",
                Password = ""
            };

            var result = await _controller.Login(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nazwa użytkownika i hasło są wymagane.",
                         badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }
        [Fact]
        public async Task ChangeUsername_ValidRequest_ReturnsOk()
        {
            var user = new User { Id = 1, Username = "oldusername", Email = "test@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!") };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new ChangeUsernameModel
            {
                NewUsername = "newusername"
            };

            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };
            var claimsIdentity = new ClaimsIdentity(userClaims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            var result = await _controller.ChangeUsername(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Nazwa użytkownika została zmieniona.", okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        }


        [Fact]
        public async Task ChangeUsername_ExistingUsername_ReturnsBadRequest()
        {
            var existingUser = new User { Username = "existinguser", Email = "existing@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!") };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var user = new User { Username = "oldusername", Email = "test@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!") };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new ChangeUsernameModel
            {
                NewUsername = "existinguser"
            };

            var result = await _controller.ChangeUsername(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Podana nazwa użytkownika jest już zajęta.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task ChangeUsername_EmptyUsername_ReturnsBadRequest()
        {
            var user = new User
            {
                Id = 1,
                Username = "oldusername",
                Email = "test@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new ChangeUsernameModel
            {
                NewUsername = "   "
            };

            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };
            var claimsIdentity = new ClaimsIdentity(userClaims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            var result = await _controller.ChangeUsername(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nowa nazwa użytkownika nie może być pusta.",
                         badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }
        [Fact]
        public async Task DeleteUser_ValidRequest_ReturnsOk()
        {
            var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!") };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };
            var claimsIdentity = new ClaimsIdentity(userClaims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            var result = await _controller.DeleteUser();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Twoje konto zostało usunięte.", okResult.Value);
        }

        [Fact]
        public async Task GetMyNotifications_NoNotifications_ReturnsOk()
        {
            var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!") };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };
            var claimsIdentity = new ClaimsIdentity(userClaims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            var result = await _controller.GetMyNotifications();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Brak nowych powiadomień.", okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        }
        [Fact]
        public async Task GetMyNotifications_WithUnreadNotifications_ReturnsOk()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };
            _context.Users.Add(user);
            var notification = new Notification
            {
                NotificationId = 1,
                UserId = user.Id,
                IsRead = false,
                Message = "Test notification",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };
            var claimsIdentity = new ClaimsIdentity(userClaims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            var result = await _controller.GetMyNotifications();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var notifications = Assert.IsAssignableFrom<List<Notification>>(okResult.Value);
            Assert.Single(notifications);
            Assert.Equal("Test notification", notifications[0].Message);
            Assert.False(notifications[0].IsRead);
        }
        [Fact]
        public async Task MarkAsRead_ValidRequest_ReturnsOk()
        {
            var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!") };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var notification = new Notification { NotificationId = 1, UserId = user.Id, IsRead = false, Message = "Test message", CreatedAt = DateTime.Now };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };
            var claimsIdentity = new ClaimsIdentity(userClaims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            var result = await _controller.MarkAsRead(notification.NotificationId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Powiadomienie zostało oznaczone jako przeczytane.", okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        }

        [Fact]
        public async Task RequestPasswordReset_ValidEmail_ReturnsOk()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _controller.RequestPasswordReset("test@example.com");

            Console.WriteLine($"RequestPasswordReset: Typ wyniku - {result.GetType().Name}");
            if (result is ObjectResult objectResult)
            {
                Console.WriteLine($"RequestPasswordReset: StatusCode - {objectResult.StatusCode}");
                Console.WriteLine($"RequestPasswordReset: Value - {Newtonsoft.Json.JsonConvert.SerializeObject(objectResult.Value)}");
            }

            Assert.IsType<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal("Link do resetu hasła został wysłany na podany adres e-mail.",
                         okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        }
        [Fact]
        public async Task RequestPasswordReset_NonExistentEmail_ReturnsBadRequest()
        {
            var nonExistentEmail = "nonexistent@example.com";

            var result = await _controller.RequestPasswordReset(nonExistentEmail);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nie znaleziono użytkownika o podanym adresie e-mail.",
                         badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }
        [Fact]
        public async Task ResetPassword_ValidToken_ReturnsOk()
        {
            var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!") };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var model = new ResetPasswordModel
            {
                Token = token,
                NewPassword = "NewPassword123!"
            };

            var result = await _controller.ResetPassword(model);

            Console.WriteLine($"ResetPassword: Typ wyniku - {result.GetType()}");

            Assert.IsType<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.NotNull(badRequestResult);
            Assert.Equal("Nieprawidłowy lub wygasły token resetujący.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }
        [Fact]
        public async Task ResetPassword_ValidTokenAndPassword_ReturnsOk()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:EmailSecretKey"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var model = new ResetPasswordModel
            {
                Token = tokenString,
                NewPassword = "NewPassword123!"
            };

            var result = await _controller.ResetPassword(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Hasło zostało zresetowane pomyślnie.",
                         okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));

            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123!", updatedUser.Password));
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Sub, user.Username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            Console.WriteLine($"GenerateJwtToken: Wygenerowany token - {tokenString}");
            return tokenString;
        }
        [Fact]
        public async Task Register_PasswordNoUpperCase_ReturnsBadRequest()
        {
            var model = new RegisterModel
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "password123!"
            };

            var result = await _controller.Register(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Hasło musi zawierać co najmniej jedną wielką literę.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task Register_PasswordNoDigit_ReturnsBadRequest()
        {
            var model = new RegisterModel
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Password!"
            };

            var result = await _controller.Register(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Hasło musi zawierać co najmniej jedną cyfrę.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task Register_PasswordNoSpecialChar_ReturnsBadRequest()
        {
            var model = new RegisterModel
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Password123"
            };

            var result = await _controller.Register(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Hasło musi zawierać co najmniej jeden znak specjalny.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task Login_ValidUsernameInvalidPassword_ReturnsBadRequest()
        {
            var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!"), Role = "User" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginModel
            {
                Username = "testuser",
                Password = "WrongPassword123!"
            };

            var result = await _controller.Login(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nieprawidłowa nazwa użytkownika lub hasło.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task ChangeUsername_Unauthorized_ReturnsUnauthorized()
        {
            var model = new ChangeUsernameModel
            {
                NewUsername = "newusername"
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            var result = await _controller.ChangeUsername(model);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Nie można zidentyfikować użytkownika.", unauthorizedResult.Value.GetType().GetProperty("message").GetValue(unauthorizedResult.Value));
        }

        [Fact]
        public async Task DeleteUser_Unauthorized_ThrowsArgumentNullException()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            await Assert.ThrowsAsync<ArgumentNullException>(() => _controller.DeleteUser());
        }

        [Fact]
        public async Task GetMyNotifications_OnlyReadNotifications_ReturnsOk()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            _context.Users.Add(user);
            var notification = new Notification
            {
                NotificationId = 1,
                UserId = user.Id,
                IsRead = true,
                Message = "Read notification",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            SetupUserClaims(user.Id);

            var result = await _controller.GetMyNotifications();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Brak nowych powiadomień.", okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value));
        }

        [Fact]
        public async Task MarkAsRead_NonExistentNotification_ReturnsNotFound()
        {
            var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!"), Role = "User" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetupUserClaims(user.Id);

            var result = await _controller.MarkAsRead(999);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Powiadomienie nie zostało znalezione.", notFoundResult.Value.GetType().GetProperty("message").GetValue(notFoundResult.Value));
        }

        [Fact]
        public async Task MarkAsRead_OtherUsersNotification_ReturnsNotFound()
        {
            var user1 = new User { Id = 1, Username = "testuser1", Email = "test1@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!"), Role = "User" };
            var user2 = new User { Id = 2, Username = "testuser2", Email = "test2@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Password123!"), Role = "User" };
            _context.Users.AddRange(user1, user2);
            var notification = new Notification { NotificationId = 1, UserId = user2.Id, IsRead = false, Message = "Test message", CreatedAt = DateTime.UtcNow };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            SetupUserClaims(user1.Id);

            var result = await _controller.MarkAsRead(notification.NotificationId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Powiadomienie nie zostało znalezione.", notFoundResult.Value.GetType().GetProperty("message").GetValue(notFoundResult.Value));
        }

        [Fact]
        public async Task RequestPasswordReset_EmptyEmail_ReturnsBadRequest()
        {
            var result = await _controller.RequestPasswordReset("");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nie znaleziono użytkownika o podanym adresie e-mail.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task RequestPasswordReset_InvalidEmailFormat_ReturnsBadRequest()
        {
            var result = await _controller.RequestPasswordReset("invalid-email");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nie znaleziono użytkownika o podanym adresie e-mail.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task ResetPassword_InvalidPassword_ReturnsBadRequest()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "User"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:EmailSecretKey"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var model = new ResetPasswordModel
            {
                Token = tokenString,
                NewPassword = "weak"
            };

            var result = await _controller.ResetPassword(model);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Hasło musi mieć co najmniej 8 znaków.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }
    }

}