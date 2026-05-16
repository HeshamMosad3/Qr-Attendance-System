using System.ComponentModel.DataAnnotations;

namespace QRAttendanceSystem.ViewModels.Enrollment
{
    public class EnrollViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public List<int> CourseIds { get; set; } = new();
    }

    public class BulkEnrollViewModel
    {
        [Required(ErrorMessage = "يرجى رفع ملف Excel")]
        public IFormFile ExcelFile { get; set; } = null!;
    }

    public class StudentEnrollmentViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? AcademicYear { get; set; }
        public List<QRAttendanceSystem.Models.Course> EnrolledCourses { get; set; } = new();
        public List<QRAttendanceSystem.Models.Course> AvailableCourses { get; set; } = new();
    }
}