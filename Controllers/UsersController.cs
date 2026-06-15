using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusBookProject.Data;
using CampusBookProject.Models;

namespace CampusBookProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    // ==================== GET ALL USERS ====================
    // GET: api/users
    // FIX: Session kontrolü eklendi - sadece giriş yapmış kullanıcılar görebilir
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Not authenticated" });

        var users = await _db.Users
            .Where(u => u.IsActive)
            .Select(u => new UserResponse
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<UserResponse>>
        {
            Success = true,
            Message = "Users retrieved successfully",
            Data = users
        });
    }

    // ==================== GET USER BY ID ====================
    // GET: api/users/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var sessionUserId = HttpContext.Session.GetInt32("UserId");
        if (!sessionUserId.HasValue)
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Not authenticated" });

        var user = await _db.Users
            .Where(u => u.UserId == id && u.IsActive)
            .Select(u => new UserResponse
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "User not found" });

        return Ok(new ApiResponse<UserResponse>
        {
            Success = true,
            Message = "User retrieved successfully",
            Data = user
        });
    }

    // ==================== UPDATE USER ====================
    // PUT: api/users/5
    // FIX: Sadece kendi profilini güncelleyebilir
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var sessionUserId = HttpContext.Session.GetInt32("UserId");
        if (!sessionUserId.HasValue)
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Not authenticated" });

        // FIX: Sadece kendi hesabını güncelleyebilir
        if (sessionUserId.Value != id)
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = "You can only update your own profile" });

        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid input data" });

        var user = await _db.Users.FindAsync(id);
        if (user == null || !user.IsActive)
            return NotFound(new ApiResponse<object> { Success = false, Message = "User not found" });

        // Email başkası tarafından kullanılıyor mu?
        var normalizedEmail = request.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail && u.UserId != id))
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Email already in use" });

        user.FullName = request.FullName.Trim();
        user.Email = normalizedEmail;

        // Session'ı güncelle
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Email", user.Email);

        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<UserResponse>
        {
            Success = true,
            Message = "User updated successfully",
            Data = new UserResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName
            }
        });
    }

    // ==================== DELETE USER ====================
    // DELETE: api/users/5
    // FIX: Sadece kendi hesabını silebilir
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var sessionUserId = HttpContext.Session.GetInt32("UserId");
        if (!sessionUserId.HasValue)
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Not authenticated" });

        // FIX: Sadece kendi hesabını silebilir
        if (sessionUserId.Value != id)
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = "You can only delete your own account" });

        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "User not found" });

        // Soft delete
        user.IsActive = false;
        await _db.SaveChangesAsync();

        // Session'ı temizle
        HttpContext.Session.Clear();

        return Ok(new ApiResponse<object> { Success = true, Message = "User deleted successfully" });
    }
}