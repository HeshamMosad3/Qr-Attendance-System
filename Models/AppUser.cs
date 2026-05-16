using Microsoft.AspNetCore.Identity;

namespace QRAttendanceSystem.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public int? AcademicYear { get; set; } // 1, 2, 3, 4
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}