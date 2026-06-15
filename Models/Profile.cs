using System.ComponentModel.DataAnnotations;

namespace CampusBookProject.Models;

// ==================== PROFILE UPDATE REQUEST ====================
public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string Email { get; set; } = string.Empty;
}

// ==================== CHANGE PASSWORD REQUEST ====================
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// ==================== PROFILE RESPONSE ====================
public class ProfileResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }

    public ProfileStats Stats { get; set; } = new ProfileStats();

    public List<BookResponse> MyBooks { get; set; } = new();
    public List<RequestResponse> SentRequests { get; set; } = new();
    public List<RequestResponse> ReceivedRequests { get; set; } = new();
}

// ==================== PROFILE STATS ====================
public class ProfileStats
{
    public int MyBooks { get; set; }
    public int SentRequests { get; set; }
    public int ReceivedRequests { get; set; }
}