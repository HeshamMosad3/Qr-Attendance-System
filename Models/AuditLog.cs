namespace QRAttendanceSystem.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }

        // Navigation
        public AppUser? User { get; set; }
    }
}