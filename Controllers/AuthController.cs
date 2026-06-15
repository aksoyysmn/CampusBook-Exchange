using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusBookProject.Data;
using CampusBookProject.Models;

namespace CampusBookProject.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    // ==================== REGISTER ====================
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid input data" });

            // Boş alan kontrolü
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "All fields are required" });
            }

            // Şifre uzunluk kontrolü
            if (request.Password.Length < 6)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Password must be at least 6 characters" });

            // Şifre eşleşme kontrolü
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Passwords do not match" });

            var normalizedUsername = request.Username.Trim().ToLower();
            var normalizedEmail = request.Email.Trim().ToLower();

            // Username kontrolü
            if (await _db.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername))
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Username already exists" });

            // Email kontrolü
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Email already exists" });

            // Yeni kullanıcı oluştur — FIX: Username küçük harfle kaydedildi (sorguyla tutarlı)
            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                Username = normalizedUsername,   // FIX: ToLower() eklendi
                PasswordHash = HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,      // FIX: UtcNow kullanıldı
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                Message = "Registration successful",
                Data = new UserResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register Error: {ex.Message}");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred during registration" });
        }
    }

    // ==================== LOGIN ====================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Username and password are required" });

            // Boş alan kontrolü
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Username and password are required" });

            var normalizedUsername = request.Username.Trim().ToLower();

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Username.ToLower() == normalizedUsername);

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid username or password" });

            if (!user.IsActive)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Account is deactivated" });

            // Son giriş zamanını güncelle
            user.LastLoginAt = DateTime.UtcNow;  // FIX: UtcNow
            await _db.SaveChangesAsync();

            // Session'a kaydet
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Email", user.Email);

            if (request.RememberMe)
                HttpContext.Session.SetString("RememberMe", "true");

            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                Message = "Login successful",
                Data = new UserResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login Error: {ex.Message}");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred during login" });
        }
    }

    // ==================== LOGOUT ====================
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        try
        {
            HttpContext.Session.Clear();
            return Ok(new ApiResponse<object> { Success = true, Message = "Logout successful" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout Error: {ex.Message}");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred during logout" });
        }
    }

    // ==================== CHECK AUTH ====================
    [HttpGet("checkauth")]
    public IActionResult CheckAuth()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId.HasValue)
            {
                return Ok(new ApiResponse<UserResponse>
                {
                    Success = true,
                    Message = "User is authenticated",
                    Data = new UserResponse
                    {
                        UserId = userId.Value,
                        Username = HttpContext.Session.GetString("Username") ?? "",
                        Email = HttpContext.Session.GetString("Email") ?? "",
                        FullName = HttpContext.Session.GetString("FullName") ?? ""
                    }
                });
            }

            return Ok(new ApiResponse<object> { Success = false, Message = "User is not authenticated", Data = null });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CheckAuth Error: {ex.Message}");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred while checking authentication" });
        }
    }

    // ==================== GET CURRENT USER ====================
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Not authenticated" });

            var user = await _db.Users.FindAsync(userId.Value);

            if (user == null || !user.IsActive)
            {
                HttpContext.Session.Clear();
                return NotFound(new ApiResponse<object> { Success = false, Message = "User not found" });
            }

            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                Message = "User retrieved successfully",
                Data = new UserResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetCurrentUser Error: {ex.Message}");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
        }
    }

    // ==================== HELPER METHODS ====================
    private string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    private bool VerifyPassword(string password, string passwordHash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, passwordHash); }
        catch (Exception ex)
        {
            Console.WriteLine($"Password verification error: {ex.Message}");
            return false;
        }
    }
}