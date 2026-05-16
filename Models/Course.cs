namespace QRAttendanceSystem.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int AcademicYear { get; set; } // 1, 2, 3, 4
        public int CreditHours { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}