using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Services
{
    public interface IEnrollmentService
    {
        Task<bool> EnrollStudentAsync(string userId, int courseId);
        Task<bool> UnenrollStudentAsync(string userId, int courseId);
        Task<List<Course>> GetStudentCoursesAsync(string userId);
        Task<List<Enrollment>> GetCourseStudentsAsync(int courseId);
        Task<(int Success, int Failed, List<string> Errors)> BulkEnrollFromExcelAsync(Stream excelStream);
    }
}