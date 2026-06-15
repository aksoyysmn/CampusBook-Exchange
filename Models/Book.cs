using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusBookProject.Models
{
    [Table("Books")]
    public class Book
    {
        [Key]
        public int BookId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Author { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? CoverImageUrl { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public User? Owner { get; set; }

        public bool CanBeBorrowed { get; set; } = true;

        public bool CanBeSwapped { get; set; } = false;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available"; // Available, Borrowed, Reserved

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // FIX: UtcNow

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ========== DTO'LAR ==========

    public class AddBookRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required")]
        [StringLength(100)]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? CoverImageBase64 { get; set; }

        public bool CanBeBorrowed { get; set; } = true;

        public bool CanBeSwapped { get; set; } = false;
    }

    public class BookResponse
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImageUrl { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public bool CanBeBorrowed { get; set; }
        public bool CanBeSwapped { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class BookDetailResponse : BookResponse
    {
        public string OwnerEmail { get; set; } = string.Empty;
    }
}