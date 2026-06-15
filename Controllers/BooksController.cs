using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusBookProject.Data;
using CampusBookProject.Models;

namespace CampusBookProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public BooksController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ========== GET ALL BOOKS ==========
        [HttpGet]
        public async Task<IActionResult> GetAllBooks([FromQuery] string? category = null)
        {
            try
            {
                IQueryable<Book> query = _db.Books
                    .Include(b => b.Owner)
                    .Where(b => b.IsActive);

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(b => b.Category == category);

                var books = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new BookResponse
                    {
                        BookId = b.BookId,
                        Title = b.Title,
                        Author = b.Author,
                        Category = b.Category,
                        Description = b.Description,
                        CoverImageUrl = b.CoverImageUrl,
                        OwnerName = b.Owner!.FullName,
                        OwnerId = b.OwnerId,
                        CanBeBorrowed = b.CanBeBorrowed,
                        CanBeSwapped = b.CanBeSwapped,
                        Status = b.Status,
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<BookResponse>>
                {
                    Success = true,
                    Message = "Books retrieved successfully",
                    Data = books
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error retrieving books: " + ex.Message });
            }
        }

        // ========== GET BOOK BY ID ==========
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            try
            {
                var book = await _db.Books
                    .Include(b => b.Owner)
                    .Where(b => b.BookId == id && b.IsActive && b.Owner != null && b.Owner.IsActive)
                    .Select(b => new BookDetailResponse
                    {
                        BookId = b.BookId,
                        Title = b.Title,
                        Author = b.Author,
                        Category = b.Category,
                        Description = b.Description,
                        CoverImageUrl = b.CoverImageUrl,
                        OwnerName = b.Owner!.FullName,
                        OwnerEmail = b.Owner.Email,
                        OwnerId = b.OwnerId,
                        CanBeBorrowed = b.CanBeBorrowed,
                        CanBeSwapped = b.CanBeSwapped,
                        Status = b.Status,
                        CreatedAt = b.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (book == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Book not found" });

                return Ok(new ApiResponse<BookDetailResponse>
                {
                    Success = true,
                    Message = "Book retrieved successfully",
                    Data = book
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error retrieving book: " + ex.Message });
            }
        }

        // ========== SEARCH BOOKS ==========
        // FIX: case-insensitive arama eklendi
        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Search query cannot be empty" });

                var lowerQuery = query.Trim().ToLower();

                var books = await _db.Books
                    .Include(b => b.Owner)
                    .Where(b => b.IsActive && b.Owner != null && b.Owner.IsActive &&
                        (b.Title.ToLower().Contains(lowerQuery) ||
                         b.Author.ToLower().Contains(lowerQuery) ||
                         b.Category.ToLower().Contains(lowerQuery)))
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new BookResponse
                    {
                        BookId = b.BookId,
                        Title = b.Title,
                        Author = b.Author,
                        Category = b.Category,
                        Description = b.Description,
                        CoverImageUrl = b.CoverImageUrl,
                        OwnerName = b.Owner!.FullName,
                        OwnerId = b.OwnerId,
                        CanBeBorrowed = b.CanBeBorrowed,
                        CanBeSwapped = b.CanBeSwapped,
                        Status = b.Status,
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<BookResponse>>
                {
                    Success = true,
                    Message = "Search completed successfully",
                    Data = books
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error searching books: " + ex.Message });
            }
        }

        // ========== ADD BOOK ==========
        [HttpPost]
        public async Task<IActionResult> AddBook([FromBody] AddBookRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "You must be logged in to add a book" });

                if (!ModelState.IsValid)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid input data" });

                // [Required] attribute'ları ModelState.IsValid tarafından kontrol edilir
                if (!request.CanBeBorrowed && !request.CanBeSwapped)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Book must be available for borrowing or swapping" });

                string? imageUrl = null;
                if (!string.IsNullOrEmpty(request.CoverImageBase64))
                    imageUrl = await SaveBase64Image(request.CoverImageBase64);

                var book = new Book
                {
                    Title = request.Title.Trim(),
                    Author = request.Author.Trim(),
                    Category = request.Category,
                    Description = request.Description?.Trim(),
                    CoverImageUrl = imageUrl,
                    OwnerId = userId.Value,
                    CanBeBorrowed = request.CanBeBorrowed,
                    CanBeSwapped = request.CanBeSwapped,
                    Status = "Available",
                    CreatedAt = DateTime.UtcNow,  // FIX: UtcNow
                    IsActive = true
                };

                _db.Books.Add(book);
                await _db.SaveChangesAsync();

                var owner = await _db.Users.FindAsync(userId.Value);

                return Ok(new ApiResponse<BookResponse>
                {
                    Success = true,
                    Message = "Book added successfully",
                    Data = new BookResponse
                    {
                        BookId = book.BookId,
                        Title = book.Title,
                        Author = book.Author,
                        Category = book.Category,
                        Description = book.Description,
                        CoverImageUrl = book.CoverImageUrl,
                        OwnerName = owner?.FullName ?? "",
                        OwnerId = book.OwnerId,
                        CanBeBorrowed = book.CanBeBorrowed,
                        CanBeSwapped = book.CanBeSwapped,
                        Status = book.Status,
                        CreatedAt = book.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error adding book: " + ex.Message });
            }
        }

        // ========== UPDATE BOOK ==========
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] AddBookRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "You must be logged in" });

                var book = await _db.Books.FindAsync(id);

                if (book == null || !book.IsActive)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Book not found" });

                if (book.OwnerId != userId.Value)
                    return StatusCode(403, new ApiResponse<object> { Success = false, Message = "You can only update your own books" });

                book.Title = request.Title.Trim();
                book.Author = request.Author.Trim();
                book.Category = request.Category;
                book.Description = request.Description?.Trim();
                book.CanBeBorrowed = request.CanBeBorrowed;
                book.CanBeSwapped = request.CanBeSwapped;
                book.UpdatedAt = DateTime.UtcNow;  // FIX: UtcNow

                if (!string.IsNullOrEmpty(request.CoverImageBase64))
                    book.CoverImageUrl = await SaveBase64Image(request.CoverImageBase64);

                await _db.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Book updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error updating book: " + ex.Message });
            }
        }

        // ========== DELETE BOOK (Soft Delete) ==========
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "You must be logged in" });

                var book = await _db.Books.FindAsync(id);

                if (book == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Book not found" });

                if (book.OwnerId != userId.Value)
                    return StatusCode(403, new ApiResponse<object> { Success = false, Message = "You can only delete your own books" });

                book.IsActive = false;
                book.UpdatedAt = DateTime.UtcNow;  // FIX: UtcNow
                await _db.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Book deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Error deleting book: " + ex.Message });
            }
        }

        // ========== HELPER: Base64 Image Save ==========
        // FIX: MIME type'a göre doğru extension belirleniyor
        private async Task<string> SaveBase64Image(string base64String)
        {
            try
            {
                string base64Data = base64String;
                string extension = ".jpg"; // varsayılan

                if (base64String.Contains(","))
                {
                    var parts = base64String.Split(',');
                    var header = parts[0]; // örn: "data:image/png;base64"
                    base64Data = parts[1];

                    if (header.Contains("png")) extension = ".png";
                    else if (header.Contains("gif")) extension = ".gif";
                    else if (header.Contains("webp")) extension = ".webp";
                    else extension = ".jpg";
                }

                var imageBytes = Convert.FromBase64String(base64Data);
                var fileName = $"{Guid.NewGuid()}{extension}";
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "books");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                return $"/uploads/books/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image save error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}