using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusBookProject.Models
{
    [Table("Requests")]
    public class Request
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }

        [Required]
        public int RequesterId { get; set; }

        [ForeignKey("RequesterId")]
        public User? Requester { get; set; }

        [Required]
        [StringLength(20)]
        public string RequestType { get; set; } = string.Empty; // "Borrow" or "Swap"

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Completed

        [StringLength(500)]
        public string? Message { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;  // FIX: UtcNow

        public DateTime? RespondedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ========== DTO'LAR ==========

    public class CreateRequestDto
    {
        [Required]
        public int BookId { get; set; }

        [Required]
        [StringLength(20)]
        public string RequestType { get; set; } = string.Empty; // "Borrow" or "Swap"

        [StringLength(500)]
        public string? Message { get; set; }
    }

    // FIX: Namespace içine taşındı
    public class RespondRequestDto
    {
        [Required]
        public string Action { get; set; } = string.Empty; // "accept" | "reject"
    }

    public class RequestResponse
    {
        public int RequestId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public string? BookCoverImageUrl { get; set; }
        public int RequesterId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterEmail { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    // FIX: UpdateRequestStatusDto kaldırıldı - hiçbir yerde kullanılmıyor
}