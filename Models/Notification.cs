namespace CampusBookProject.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "NewRequest", "Approved", "Rejected"
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}