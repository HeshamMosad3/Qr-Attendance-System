using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public EnrollmentService(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<bool> EnrollStudentAsync(string userId, int courseId)
        {
            bool exists = await _context.Enrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
            if (exists) return false;

            var user = await _context.Users.FindAsync(userId);
            _context.Enrollments.Add(new Enrollment
            {
                UserId = userId,
                CourseId = courseId,
                AcademicYear = user?.AcademicYear ?? 1,
                EnrolledAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnenrollStudentAsync(string userId, int courseId)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
            if (enrollment == null) return false;

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Course>> GetStudentCoursesAsync(string userId)
        {
            return await _context.Enrollments
                .Where(e => e.UserId == userId)
                .Include(e => e.Course)
                .Select(e => e.Course)
                .ToListAsync();
        }

        public async Task<List<Enrollment>> GetCourseStudentsAsync(int courseId)
        {
            return await _context.Enrollments
                .Where(e => e.CourseId == courseId)
                .Include(e => e.User)
                .ToListAsync();
        }

        public async Task<(int Success, int Failed, List<string> Errors)> BulkEnrollFromExcelAsync(Stream excelStream)
        {
            int success = 0, failed = 0;
            var errors = new List<string>();

            using var workbook = new XLWorkbook(excelStream);
            var sheet = workbook.Worksheet(1);
            var rows = sheet.RangeUsed().RowsUsed().Skip(1); // تخطى الـ Header

            foreach (var row in rows)
            {
                try
                {
                    string email = row.Cell(1).GetString().Trim();
                    string courseCode = row.Cell(2).GetString().Trim();

                    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(courseCode))
                    {
                        errors.Add($"صف {row.RowNumber()}: بيانات ناقصة");
                        failed++;
                        continue;
                    }

                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        errors.Add($"الصف {row.RowNumber()}: المستخدم '{email}' غير موجود");
                        failed++;
                        continue;
                    }

                    var course = await _context.Courses
                        .FirstOrDefaultAsync(c => c.Code == courseCode);
                    if (course == null)
                    {
                        errors.Add($"الصف {row.RowNumber()}: المادة '{courseCode}' غير موجودة");
                        failed++;
                        continue;
                    }

                    bool enrolled = await EnrollStudentAsync(user.Id, course.Id);
                    if (enrolled) success++;
                    else
                    {
                        errors.Add($"الصف {row.RowNumber()}: {email} مسجل مسبقاً في {courseCode}");
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"الصف {row.RowNumber()}: خطأ — {ex.Message}");
                    failed++;
                }
            }

            return (success, failed, errors);
        }
    }
}