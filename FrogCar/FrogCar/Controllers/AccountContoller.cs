using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FrogCar.Data;
using FrogCar.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using FrogCar.Constants; 
using System;
using System.Linq;

namespace FrogCar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordValidator _passwordValidator;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            AppDbContext context,
            IPasswordValidator passwordValidator,
            IJwtTokenService jwtTokenService,
            EmailService emailService,
            ILogger<AccountController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _passwordValidator = passwordValidator ?? throw new ArgumentNullException(nameof(passwordValidator));
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            _logger.LogInformation("Próba logowania dla użytkownika: {Username}", model?.Username);

            if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                _logger.LogWarning("Nieprawidłowe dane logowania: {Username}, {Password}", model?.Username, model?.Password);
                return BadRequest(new { message = ErrorMessages.InvalidLoginData });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null)
            {
                _logger.LogWarning("Użytkownik {Username} nie istnieje", model.Username);
                return BadRequest(new { message = ErrorMessages.InvalidCredentials });
            }

            if (!_passwordValidator.Verify(model.Password, user.Password))
            {
                _logger.LogWarning("Nieprawidłowe hasło dla użytkownika: {Username}", model.Username);
                return BadRequest(new { message = ErrorMessages.InvalidCredentials });
            }

            var token = _jwtTokenService.GenerateToken(user);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Generowanie tokenu JWT nie powiodło się dla użytkownika: {Username}", model.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Błąd generowania tokenu." });
            }

            SetJwtCookie(token);
            _logger.LogInformation("Logowanie pomyślne dla użytkownika: {Username}", model.Username);

            return Ok(new { message = "Logowanie udane", token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            _logger.LogInformation("Rozpoczęto rejestrację dla użytkownika: {Username}", model?.Username);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Błąd walidacji modelu: {Errors}", string.Join(", ", errors));
                return BadRequest(new { message = ErrorMessages.InvalidModel, errors });
            }

            var passwordError = _passwordValidator.Validate(model.Password);
            if (!string.IsNullOrEmpty(passwordError))
            {
                _logger.LogWarning("Błąd walidacji hasła: {Error}", passwordError);
                return BadRequest(new { message = passwordError });
            }

            if (await IsUsernameTaken(model.Username))
            {
                _logger.LogWarning("Nazwa użytkownika zajęta: {Username}", model.Username);
                return BadRequest(new { message = ErrorMessages.UsernameTaken });
            }

            if (await IsEmailTaken(model.Email))
            {
                _logger.LogWarning("Email zajęty: {Email}", model.Email);
                return BadRequest(new { message = ErrorMessages.EmailTaken });
            }

            var newUser = CreateUser(model);
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Użytkownik {Username} zarejestrowany pomyślnie", model.Username);

            return Ok(new { message = "Rejestracja zakończona sukcesem" });
        }

        [HttpPut("change-username")]
        [Authorize]
        public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameModel model)
        {
            _logger.LogInformation("Próba zmiany nazwy użytkownika");
            if (string.IsNullOrWhiteSpace(model.NewUsername))
            {
                _logger.LogWarning("Pusta nazwa użytkownika");
                return BadRequest(new { message = ErrorMessages.EmptyUsername });
            }

            if (await IsUsernameTaken(model.NewUsername))
            {
                _logger.LogWarning("Nazwa użytkownika zajęta: {NewUsername}", model.NewUsername);
                return BadRequest(new { message = ErrorMessages.UsernameTaken });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Brak ID użytkownika w tokenie JWT");
                return Unauthorized(new { message = ErrorMessages.Unauthorized });
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                _logger.LogWarning("Użytkownik o ID {UserId} nie istnieje", userId);
                return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.UserNotFound });
            }

            user.Username = model.NewUsername;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Nazwa użytkownika zmieniona na: {NewUsername}", model.NewUsername);

            return Ok(new { message = "Nazwa użytkownika została zmieniona." });
        }

        [HttpDelete("delete")]
        [Authorize]
        public async Task<IActionResult> DeleteUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            _logger.LogInformation("Próba usunięcia użytkownika o ID: {UserId}", userId);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Użytkownik o ID {UserId} nie istnieje", userId);
                return StatusCode(StatusCodes.Status404NotFound, new { message = ErrorMessages.UserNotFound });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Użytkownik o ID {UserId} usunięty", userId);

            return Ok("Twoje konto zostało usunięte.");
        }

        [HttpGet("Notification")]
        [Authorize]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("Pobieranie powiadomień dla użytkownika o ID: {UserId}", userId);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Brak ID użytkownika w tokenie JWT");
                return Unauthorized(new { message = ErrorMessages.Unauthorized });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == int.Parse(userId) && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Include(n => n.User)
                .ToListAsync();

            if (!notifications.Any())
            {
                _logger.LogInformation("Brak nowych powiadomień dla użytkownika o ID: {UserId}", userId);
                return Ok(new { message = ErrorMessages.NoNotifications });
            }

            _logger.LogInformation("Pobrano {Count} powiadomień dla użytkownika o ID: {UserId}", notifications.Count, userId);
            return Ok(notifications);
        }

        [HttpPatch("Notification/{notificationId}")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("Próba oznaczenia powiadomienia {NotificationId} jako przeczytanego", notificationId);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Brak ID użytkownika w tokenie JWT");
                return Unauthorized(new { message = ErrorMessages.Unauthorized });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == int.Parse(userId));

            if (notification == null)
            {
                _logger.LogWarning("Powiadomienie o ID {NotificationId} nie istnieje lub nie należy do użytkownika {UserId}", notificationId, userId);
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Powiadomienie nie zostało znalezione." });
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Powiadomienie o ID {NotificationId} oznaczone jako przeczytane", notificationId);

            return Ok(new { message = "Powiadomienie zostało oznaczone jako przeczytane." });
        }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] string email)
        {
            _logger.LogInformation("Próba żądania resetu hasła dla email: {Email}", email);
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Pusty adres email w żądaniu resetu hasła");
                return BadRequest(new { message = "Adres email jest wymagany." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                _logger.LogWarning("Nie znaleziono użytkownika dla email: {Email}", email);
                return Ok(new { message = "Jeśli podany adres e-mail istnieje w naszej bazie, link do resetu hasła został wysłany." });
            }

            var resetToken = _jwtTokenService.GeneratePasswordResetToken(user);
            var resetLink = _jwtTokenService.BuildResetLink(resetToken);

            try
            {
                await SendPasswordResetEmail(user, resetLink);
                _logger.LogInformation("Wysłano link resetu hasła dla użytkownika: {Username}", user.Username);
                return Ok(new { message = "Link do resetu hasła został wysłany na podany adres e-mail." });
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "Błąd SMTP podczas wysyłania e-maila resetu hasła do {Email}: {Message}", email, smtpEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Nie udało się wysłać e-maila z linkiem do resetu hasła.", error = smtpEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ogólny podczas wysyłania e-maila resetu hasła do {Email}: {Message}", email, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Nie udało się wysłać e-maila z linkiem do resetu hasła.", error = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            _logger.LogInformation("Próba resetu hasła");
            try
            {
                var userId = _jwtTokenService.ValidatePasswordResetToken(model.Token);
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Nieprawidłowy token resetu hasła (użytkownik nie istnieje lub token jest zły)");
                    return BadRequest(new { message = ErrorMessages.InvalidToken });
                }

                var passwordError = _passwordValidator.Validate(model.NewPassword);
                if (!string.IsNullOrEmpty(passwordError))
                {
                    _logger.LogWarning("Błąd walidacji nowego hasła: {Error}", passwordError);
                    return BadRequest(new { message = passwordError });
                }

                user.Password = _passwordValidator.Hash(model.NewPassword);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Hasło zresetowane pomyślnie dla użytkownika o ID: {UserId}", userId);

                return Ok(new { message = "Hasło zostało zresetowane pomyślnie." });
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Nieprawidłowy lub wygasły token resetu hasła: {Message}", ex.Message);
                return BadRequest(new { message = ErrorMessages.InvalidToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ogólny podczas resetowania hasła: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Wystąpił błąd podczas resetowania hasła." });
            }
        }

        private async Task<bool> IsUsernameTaken(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        private async Task<bool> IsEmailTaken(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        private User CreateUser(RegisterModel model)
        {
            return new User
            {
                Username = model.Username,
                Email = model.Email,
                Password = _passwordValidator.Hash(model.Password),
                Role = Roles.User 
            };
        }

        private void SetJwtCookie(string token)
        {
            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(10)
            });
        }

        private async Task SendPasswordResetEmail(User user, string resetLink)
        {
            var emailBody = $@"
                <p>Cześć {user.Username},</p>
                <p>Otrzymaliśmy prośbę o zresetowanie Twojego hasła. Kliknij poniższy link, aby ustawić nowe hasło:</p>
                <p><a href='{resetLink}'>Resetuj hasło</a></p>
                <p>Jeśli to nie Ty, zignoruj tę wiadomość.</p>";
            await _emailService.SendEmailAsync(user.Email, "Resetowanie hasła", emailBody);
        }
    }


    public interface IPasswordValidator
    {
        string Validate(string password);
        string Hash(string password);
        bool Verify(string inputPassword, string storedHash);
    }

    public class PasswordValidator : IPasswordValidator
    {
        private const string SpecialCharacters = "!@#$%^&*()-_=+[]{}|;:'\",.<>?/";

        public string Validate(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return "Hasło nie może być puste.";
            if (password.Length < 8)
                return "Hasło musi mieć co najmniej 8 znaków.";
            if (!password.Any(char.IsUpper))
                return "Hasło musi zawierać co najmniej jedną wielką literę.";
            if (!password.Any(char.IsDigit))
                return "Hasło musi zawierać co najmniej jedną cyfrę.";
            if (!password.Any(c => SpecialCharacters.Contains(c)))
                return "Hasło musi zawierać co najmniej jeden znak specjalny.";
            return null;
        }

        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool Verify(string inputPassword, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
        }
    }

    public interface IJwtTokenService
    {
        string GenerateToken(User user);
        string GeneratePasswordResetToken(User user);
        int ValidatePasswordResetToken(string token);
        string BuildResetLink(string resetToken);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string GenerateToken(User user)
        {
            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("Klucz JWT nie jest skonfigurowany.");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
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

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GeneratePasswordResetToken(User user)
        {
            var key = _configuration["Jwt:EmailSecretKey"];
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("Klucz EmailSecretKey nie jest skonfigurowany.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public int ValidatePasswordResetToken(string token)
        {
            var key = _configuration["Jwt:EmailSecretKey"];
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("Klucz EmailSecretKey nie jest skonfigurowany.");

            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = handler.ValidateToken(token, parameters, out _);
                return int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }
            catch (Exception ex)
            {
                throw new SecurityTokenException("Nieprawidłowy lub wygasły token", ex);
            }
        }

        public string BuildResetLink(string resetToken)
        {
            var frontendUrl = _configuration["Frontend:ResetPasswordUrl"];
            if (string.IsNullOrEmpty(frontendUrl))
                throw new InvalidOperationException(ErrorMessages.FrontendUrlMissing);
            return $"{frontendUrl}?token={resetToken}";
        }
    }
}