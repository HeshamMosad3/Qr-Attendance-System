using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;
using QRAttendanceSystem.Services;
using QRAttendanceSystem.ViewModels.Enrollment;

namespace QRAttendanceSystem.Controllers
{
    [Authorize(Roles = "Admin,Doctor")]
    public class EnrollmentController : Controller
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public EnrollmentController(IEnrollmentService enrollmentService,
            AppDbContext context, UserManager<AppUser> userManager)
        {
            _enrollmentService = enrollmentService;
            _context = context;
            _userManager = userManager;
        }

        // عرض قائمة الطلاب مع مواد كل طالب
        public async Task<IActionResult> Index(string? search)
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            if (!string.IsNullOrEmpty(search))
                students = students.Where(u =>
                    u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            ViewBag.Search = search;
            return View(students);
        }

        // تفاصيل تسجيل طالب بعينه
        public async Task<IActionResult> StudentCourses(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var enrolled = await _enrollmentService.GetStudentCoursesAsync(userId);
            var allCourses = await _context.Courses.Where(c => c.IsActive).ToListAsync();
            var available = allCourses.Where(c => enrolled.All(e => e.Id != c.Id)).ToList();

            var vm = new StudentEnrollmentViewModel
            {
                UserId = userId,
                StudentName = user.FullName,
                Email = user.Email ?? "",
                AcademicYear = user.AcademicYear,
                EnrolledCourses = enrolled,
                AvailableCourses = available
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(string userId, int courseId)
        {
            bool success = await _enrollmentService.EnrollStudentAsync(userId, courseId);
            TempData[success ? "Success" : "Error"] = success
                ? "تم التسجيل في المادة بنجاح."
                : "الطالب مسجل مسبقاً في هذه المادة.";
            return RedirectToAction("StudentCourses", new { userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unenroll(string userId, int courseId)
        {
            bool success = await _enrollmentService.UnenrollStudentAsync(userId, courseId);
            TempData[success ? "Success" : "Error"] = success
                ? "تم إلغاء التسجيل."
                : "فشل إلغاء التسجيل.";
            return RedirectToAction("StudentCourses", new { userId });
        }

        // Bulk Import
        [HttpGet]
        public IActionResult BulkImport() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkImport(BulkEnrollViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.ExcelFile.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                && !model.ExcelFile.FileName.EndsWith(".xlsx"))
            {
                TempData["Error"] = "يرجى رفع ملف Excel (.xlsx) فقط.";
                return View(model);
            }

            using var stream = model.ExcelFile.OpenReadStream();
            var (success, failed, errors) = await _enrollmentService.BulkEnrollFromExcelAsync(stream);

            TempData["BulkSuccess"] = success;
            TempData["BulkFailed"] = failed;
            TempData["BulkErrors"] = string.Join("|", errors);

            return RedirectToAction("BulkResult");
        }

        public IActionResult BulkResult()
        {
            ViewBag.Success = TempData["BulkSuccess"];
            ViewBag.Failed = TempData["BulkFailed"];
            var errStr = TempData["BulkErrors"] as string ?? "";
            ViewBag.Errors = string.IsNullOrEmpty(errStr)
                ? new List<string>()
                : errStr.Split('|').ToList();
            return View();
        }
    }
}