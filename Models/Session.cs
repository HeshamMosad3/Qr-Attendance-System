namespace QRAttendanceSystem.Models
{
    public enum SessionType
    {
        Lecture,
        Section,
        Lab,
        Exam
    }

    public class Session
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public SessionType Type { get; set; }
        public int CourseId { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public string QrToken { get; set; } = string.Empty;
        public DateTime QrExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime SessionDate { get; set; }
        public int LateLimitMinutes { get; set; } = 15; // بعد كام دقيقة يعتبر Late

        // Navigation
        public Course Course { get; set; } = null!;
        public AppUser CreatedBy { get; set; } = null!;
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}