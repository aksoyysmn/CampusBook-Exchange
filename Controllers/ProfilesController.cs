using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusBookProject.Data;
using CampusBookProject.Models;

namespace CampusBookProject.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfilesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProfilesController(AppDbContext db)
    {
        _db = db;
    }

    // ==================== GET USER PROFILE ====================
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Please login first" });

            var user = await _db.Users.FindAsync(userId.Value);
            if (user == null || !user.IsActive)
                return NotFound(new ApiResponse<object> { Success = false, Message = "User not found" });

            // ==================== MY BOOKS ====================
            var myBooks = await _db.Books
                .Where(b => b.OwnerId == userId.Value && b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BookResponse
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Category = b.Category,
                    Description = b.Description,
                    CoverImageUrl = b.CoverImageUrl,
                    OwnerId = b.OwnerId,
                    OwnerName = user.FullName,
                    CanBeBorrowed = b.CanBeBorrowed,
                    CanBeSwapped = b.CanBeSwapped,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            // ==================== SENT REQUESTS ====================
            var sentRequests = await _db.Requests
                .Where(r => r.RequesterId == userId.Value && r.IsActive
                         && r.Book != null && r.Book.IsActive)  // FIX: IsActive kontrolü eklendi
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new RequestResponse
                {
                    RequestId = r.RequestId,
                    BookId = r.BookId,
                    BookTitle = r.Book != null ? r.Book.Title : "Unknown",
                    BookAuthor = r.Book != null ? r.Book.Author : "-",
                    BookCoverImageUrl = r.Book != null ? r.Book.CoverImageUrl : null,
                    RequesterId = r.RequesterId,
                    RequesterName = r.Requester != null ? r.Requester.FullName : "-",
                    RequesterEmail = r.Requester != null ? r.Requester.Email : "-",
                    OwnerId = r.Book != null ? r.Book.OwnerId : 0,
                    OwnerName = r.Book != null && r.Book.Owner != null ? r.Book.Owner.FullName : "-",
                    RequestType = r.RequestType,
                    Status = r.Status,
                    Message = r.Message,
                    RequestedAt = r.RequestedAt,
                    RespondedAt = r.RespondedAt
                })
                .ToListAsync();

            // ==================== RECEIVED REQUESTS ====================
            var receivedRequests = await _db.Requests
                .Where(r => r.Book != null
                         && r.Book.OwnerId == userId.Value
                         && r.Book.IsActive  // FIX: IsActive kontrolü eklendi
                         && r.IsActive)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new RequestResponse
                {
                    RequestId = r.RequestId,
                    BookId = r.BookId,
                    BookTitle = r.Book != null ? r.Book.Title : "Unknown",
                    BookAuthor = r.Book != null ? r.Book.Author : "-",
                    BookCoverImageUrl = r.Book != null ? r.Book.CoverImageUrl : null,
                    RequesterId = r.RequesterId,
                    RequesterName = r.Requester != null ? r.Requester.FullName : "-",
                    RequesterEmail = r.Requester != null ? r.Requester.Email : "-",
                    OwnerId = r.Book != null ? r.Book.OwnerId : 0,
                    OwnerName = r.Book != null && r.Book.Owner != null ? r.Book.Owner.FullName : "-",
                    RequestType = r.RequestType,
                    Status = r.Status,
                    Message = r.Message,
                    RequestedAt = r.RequestedAt,
                    RespondedAt = r.RespondedAt
                })
                .ToListAsync();

            var profileData = new ProfileResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive,
                Stats = new ProfileStats
                {
                    MyBooks = myBooks.Count,
                    SentRequests = sentRequests.Count,
                    ReceivedRequests = receivedRequests.Count
                },
                MyBooks = myBooks,
                SentRequests = sentRequests,
                ReceivedRequests = receivedRequests
            };

            return Ok(new ApiResponse<ProfileResponse>
            {
                Success = true,
                Message = "Profile retrieved successfully",
                Data = profileData
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetProfile Error: {ex.Message}");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred while retrieving profile" });
        }
    }

    // ==================== UPDATE PROFILE ====================
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Please login first" });

            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid input data" });

            var user = await _db.Users.FindAsync(userId.Value);
            if (user == null || !user.IsActive)
                return NotFound(new ApiResponse<object> { Success = false, Message = "User not found" });

            bool updated = false;

            if (request.FullName.Trim() != user.FullName)
            {
                user.FullName = request.FullName.Trim();
                updated = true;
            }

            var normalizedEmail = request.Email.Trim().ToLower();
            if (normalizedEmail != user.Email.ToLower())
            {
                var emailExists = await _db.Users.AnyAsync(u =>
                    u.Email.ToLower() == normalizedEmail && u.UserId != userId.Value);

                if (emailExists)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Email already in use" });

                user.Email = normalizedEmail;
                updated = true;
            }

            if (updated)
            {
                await _db.SaveChangesAsync();
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Email", user.Email);
            }

            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                Message = updated ? "Profile updated successfully" : "No changes made",
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
            Console.WriteLine($"UpdateProfile Error: {ex.Message}");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred while updating profile" });
        }
    }

    // ==================== CHANGE PASSWORD ====================
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Please login first" });

            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid input data" });

            var user = await _db.Users.FindAsync(userId.Value);
            if (user == null || !user.IsActive)
                return NotFound(new ApiResponse<object> { Success = false, Message = "User not found" });

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Current password is incorrect" });

            user.PasswordHash = HashPassword(request.NewPassword);
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<object> { Success = true, Message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChangePassword Error: {ex.Message}");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred while changing password" });
        }
    }

    // ==================== HELPER METHODS ====================
    private string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    private bool VerifyPassword(string password, string passwordHash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, passwordHash); }
        catch { return false; }
    }
}