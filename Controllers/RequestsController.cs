using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusBookProject.Data;
using CampusBookProject.Models;

namespace CampusBookProject.Controllers
{
    [ApiController]
    [Route("api/requests")]
    public class RequestsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RequestsController(AppDbContext db)
        {
            _db = db;
        }

        // ========== CREATE REQUEST ==========
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto dto)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "You must be logged in" });

                if (!ModelState.IsValid)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid input data" });

                var book = await _db.Books
                    .Include(b => b.Owner)
                    .FirstOrDefaultAsync(b => b.BookId == dto.BookId && b.IsActive);

                if (book == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Book not found" });

                if (book.OwnerId == userId.Value)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "You cannot request your own book" });

                if (book.Status != "Available")
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Book is not available" });

                var requestType = dto.RequestType?.Trim() ?? string.Empty;

                if (requestType != "Borrow" && requestType != "Swap")
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid request type" });

                if (requestType == "Borrow" && !book.CanBeBorrowed)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "This book cannot be borrowed" });

                if (requestType == "Swap" && !book.CanBeSwapped)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "This book cannot be swapped" });

                var existingRequest = await _db.Requests
                    .AnyAsync(r => r.BookId == dto.BookId
                        && r.RequesterId == userId.Value
                        && r.Status == "Pending"
                        && r.IsActive);

                if (existingRequest)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "You already have a pending request for this book" });

                var request = new Request
                {
                    BookId = dto.BookId,
                    RequesterId = userId.Value,
                    RequestType = requestType,
                    Message = dto.Message,
                    Status = "Pending",
                    RequestedAt = DateTime.UtcNow,  // FIX: UtcNow
                    IsActive = true
                };

                _db.Requests.Add(request);
                book.Status = "Reserved";
                // ===== KİTAP SAHİBİNE BİLDİRİM OLUŞTUR =====
                var requester = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
                _db.Notifications.Add(new Notification
                {
                    UserId = book.OwnerId,
                    Type = "NewRequest",
                    Message = $"{requester?.FullName ?? "A student"} sent a {requestType.ToLower()} request for your book \"{book.Title}\".",
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"{requestType} request sent successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error creating request: " + ex.Message });
            }
        }

        // ========== GET MY (SENT) REQUESTS ==========
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "You must be logged in" });

                var requests = await _db.Requests
                    .Include(r => r.Book).ThenInclude(b => b!.Owner)
                    .Include(r => r.Requester)
                    .Where(r => r.RequesterId == userId.Value && r.IsActive
                             && r.Book != null && r.Book.IsActive)
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

                return Ok(new ApiResponse<List<RequestResponse>>
                {
                    Success = true,
                    Message = "Requests retrieved successfully",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error retrieving requests: " + ex.Message });
            }
        }

        // ========== GET RECEIVED REQUESTS ==========
        [HttpGet("received-requests")]
        public async Task<IActionResult> GetReceivedRequests()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "You must be logged in" });

                var requests = await _db.Requests
                    .Include(r => r.Book).ThenInclude(b => b!.Owner)
                    .Include(r => r.Requester)
                    .Where(r => r.Book != null && r.Book.OwnerId == userId.Value
                             && r.Book.IsActive && r.IsActive)
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

                return Ok(new ApiResponse<List<RequestResponse>>
                {
                    Success = true,
                    Message = "Received requests retrieved successfully",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error retrieving requests: " + ex.Message });
            }
        }

        // ========== RESPOND TO REQUEST (Accept / Reject) ==========
        [HttpPut("{id}/respond")]
        public async Task<IActionResult> RespondToRequest(int id, [FromBody] RespondRequestDto dto)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "You must be logged in" });

                if (dto == null || string.IsNullOrWhiteSpace(dto.Action))
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Action is required (accept or reject)" });

                var action = dto.Action.Trim().ToLower();

                if (action != "accept" && action != "reject")
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Action must be 'accept' or 'reject'" });

                var request = await _db.Requests
                .Include(r => r.Book).ThenInclude(b => b!.Owner)
                .FirstOrDefaultAsync(r => r.RequestId == id && r.IsActive);

                if (request == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Request not found" });

                if (request.Book == null || request.Book.OwnerId != userId.Value)
                    return StatusCode(403, new ApiResponse<object> { Success = false, Message = "You are not authorized to respond to this request" });

                if (request.Status != "Pending")
                    return BadRequest(new ApiResponse<object> { Success = false, Message = $"This request has already been {request.Status.ToLower()}" });

                if (action == "accept")
                {
                    request.Status = "Approved";
                    request.Book.Status = "Borrowed";
                }
                else
                {
                    request.Status = "Rejected";
                    request.Book.Status = "Available";
                }

                request.RespondedAt = DateTime.UtcNow;  // FIX: UtcNow
                                                        // ===== BİLDİRİM OLUŞTUR =====
                if (action == "accept")
                {
                    // İstek gönderen kişiye: "kitabın kabul edildi"
                    _db.Notifications.Add(new Notification
                    {
                        UserId = request.RequesterId,
                        Type = "Approved",
                        Message = $"{request.Book!.Owner?.FullName ?? "Book owner"} accepted your {request.RequestType.ToLower()} request for \"{request.Book.Title}\".",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    // İstek gönderen kişiye: "kitabın reddedildi"
                    _db.Notifications.Add(new Notification
                    {
                        UserId = request.RequesterId,
                        Type = "Rejected",
                        Message = $"{request.Book!.Owner?.FullName ?? "Book owner"} rejected your {request.RequestType.ToLower()} request for \"{request.Book.Title}\".",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await _db.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Request {request.Status.ToLower()} successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error responding to request: " + ex.Message });
            }
        }

        // ========== CANCEL SENT REQUEST ==========
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "You must be logged in" });

                var request = await _db.Requests
                    .Include(r => r.Book)
                    .FirstOrDefaultAsync(r => r.RequestId == id && r.IsActive);

                if (request == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Request not found" });

                if (request.RequesterId != userId.Value)
                    return StatusCode(403, new ApiResponse<object> { Success = false, Message = "You are not authorized to cancel this request" });

                if (request.Status != "Pending")
                    return BadRequest(new ApiResponse<object> { Success = false, Message = $"Only pending requests can be cancelled. Current status: {request.Status}" });

                request.IsActive = false;
                request.RespondedAt = DateTime.UtcNow;  // FIX: UtcNow

                var hasOtherPending = await _db.Requests.AnyAsync(r =>
                    r.RequestId != request.RequestId &&
                    r.BookId == request.BookId &&
                    r.IsActive &&
                    r.Status == "Pending");

                if (!hasOtherPending && request.Book != null && request.Book.Status == "Reserved")
                    request.Book.Status = "Available";

                await _db.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Request cancelled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error cancelling request: " + ex.Message });
            }
        }

        // ========== COMPLETE REQUEST ==========
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteRequest(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "You must be logged in" });

                var request = await _db.Requests
                    .Include(r => r.Book)
                    .FirstOrDefaultAsync(r => r.RequestId == id && r.IsActive);

                if (request == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Request not found" });

                if (request.Book == null || request.Book.OwnerId != userId.Value)
                    return StatusCode(403, new ApiResponse<object> { Success = false, Message = "You are not authorized to perform this action" });

                if (request.Status != "Approved")
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Only approved requests can be completed" });

                request.Status = "Completed";
                request.CompletedAt = DateTime.UtcNow;  // FIX: UtcNow
                request.Book.Status = "Available";

                await _db.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Request completed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error completing request: " + ex.Message });
            }
        }

        // ========== DELETE REQUEST ==========
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "You must be logged in" });

                var request = await _db.Requests
                    .Include(r => r.Book)
                    .FirstOrDefaultAsync(r => r.RequestId == id && r.IsActive);

                if (request == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Request not found" });

                if (request.RequesterId != userId.Value)
                    return StatusCode(403, new ApiResponse<object> { Success = false, Message = "You are not authorized to perform this action" });

                if (request.Status != "Pending")
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Only pending requests can be cancelled" });

                request.IsActive = false;

                var hasOtherPending = await _db.Requests.AnyAsync(r =>
                    r.RequestId != request.RequestId &&
                    r.BookId == request.BookId &&
                    r.IsActive &&
                    r.Status == "Pending");

                if (!hasOtherPending && request.Book != null && request.Book.Status == "Reserved")
                    request.Book.Status = "Available";

                await _db.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Request cancelled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error cancelling request: " + ex.Message });
            }
        }
    }
}