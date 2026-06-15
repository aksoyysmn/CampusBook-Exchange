using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusBookProject.Data;
using CampusBookProject.Models;

namespace CampusBookProject.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db)
        {
            _db = db;
        }

        // GET: okunmamış bildirimleri getir
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Not logged in" });

            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new {
                    n.NotificationId,
                    n.Message,
                    n.Type,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(new ApiResponse<object> { Success = true, Data = notifications });
        }

        // PUT: tüm bildirimleri okundu işaretle
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Not logged in" });

            var unread = await _db.Notifications
                .Where(n => n.UserId == userId.Value && !n.IsRead)
                .ToListAsync();

            unread.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<object> { Success = true, Message = "All marked as read" });
        }

        // PUT: tek bildirimi okundu işaretle
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Not logged in" });

            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId.Value);

            if (notification == null)
                return NotFound(new ApiResponse<object> { Success = false, Message = "Not found" });

            notification.IsRead = true;
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<object> { Success = true });
        }
    }
}