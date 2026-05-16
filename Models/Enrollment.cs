namespace QRAttendanceSystem.Models
{
    public class Enrollment
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public int AcademicYear { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public AppUser User { get; set; } = null!;
        public Course Course { get; set; } = null!;
    }
}