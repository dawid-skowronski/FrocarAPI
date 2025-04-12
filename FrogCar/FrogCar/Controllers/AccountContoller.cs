using Microsoft.AspNetCore.Mvc;
using FrogCar.Models;
using FrogCar.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;

namespace FrogCar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        
        [HttpPost("register")]
        
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { message = "Błąd walidacji.", errors });
            }

            var passwordError = ValidatePassword(model.Password);
            if (!string.IsNullOrEmpty(passwordError))
            {
                return BadRequest(new { message = passwordError });
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Nazwa użytkownika jest już zajęta." });
            }

            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingEmail != null)
            {
                return BadRequest(new { message = "Podany adres e-mail jest już używany." });
            }

            string role = "User";

            var newUser = new User
            {
                Username = model.Username,
                Email = model.Email,
                Password = HashPassword(model.Password), 
                Role = role
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rejestracja zakończona sukcesem" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            Console.WriteLine(JsonSerializer.Serialize(model)); 

            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new { message = "Nazwa użytkownika i hasło są wymagane." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null)
            {
                return BadRequest(new { message = "Nieprawidłowa nazwa użytkownika lub hasło." });
            }

            if (!VerifyPassword(model.Password, user.Password))
            {
                return BadRequest(new { message = "Nieprawidłowa nazwa użytkownika lub hasło." });
            }

            var token = GenerateJwtToken(user);
            return Ok(new { message = "Logowanie udane", token });
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

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
        }


        private string ValidatePassword(string password)
        {
            if (password.Length < 8)
            {
                return "Hasło musi mieć co najmniej 8 znaków.";
            }

            if (!password.Any(char.IsUpper))
            {
                return "Hasło musi zawierać co najmniej jedną wielką literę.";
            }

            if (!password.Any(char.IsDigit))
            {
                return "Hasło musi zawierać co najmniej jedną cyfrę.";
            }

            var specialCharacters = "!@#$%^&*()-_=+[]{}|;:'\",.<>?/";
            if (!password.Any(c => specialCharacters.Contains(c)))
            {
                return "Hasło musi zawierać co najmniej jeden znak specjalny.";
            }

            return null; 
        }

        [HttpPut("change-username")]
        [Authorize]
        public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NewUsername))
            {
                return BadRequest(new { message = "Nowa nazwa użytkownika nie może być pusta." });
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.NewUsername);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Podana nazwa użytkownika jest już zajęta." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized(new { message = "Nie można zidentyfikować użytkownika." });
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new { message = "Użytkownik nie istnieje." });
            }

            user.Username = model.NewUsername;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Nazwa użytkownika została zmieniona." });
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var user = await _context.Users.FindAsync(userId);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok("Twoje konto zostało usunięte.");
        }


    }
}