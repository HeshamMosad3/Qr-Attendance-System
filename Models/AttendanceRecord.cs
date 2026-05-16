namespace QRAttendanceSystem.Models
{
    public enum AttendanceStatus
    {
        OnTime,
        Late
    }

    public class AttendanceRecord
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int SessionId { get; set; }
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
        public AttendanceStatus Status { get; set; }

        // Navigation
        public AppUser User { get; set; } = null!;
        public Session Session { get; set; } = null!;
    }
}