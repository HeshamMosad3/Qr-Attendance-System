using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;
using QRAttendanceSystem.ViewModels.Admin;

namespace QRAttendanceSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(AppDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");

            var courseStats = await _context.Courses
                .Select(c => new CourseAttendanceStat
                {
                    CourseName = c.Name,
                    TotalSessions = c.Sessions.Count,
                    TotalAttendance = c.Sessions.SelectMany(s => s.AttendanceRecords).Count(),
                    AttendanceRate = c.Sessions.Count == 0 ? 0 :
                        Math.Round((double)c.Sessions.SelectMany(s => s.AttendanceRecords).Count()
                            / (c.Sessions.Count * students.Count) * 100, 1)
                })
                .ToListAsync();

            var vm = new DashboardViewModel
            {
                TotalStudents = students.Count,
                TotalDoctors = doctors.Count,
                TotalSessions = await _context.Sessions.CountAsync(),
                TotalAttendanceRecords = await _context.AttendanceRecords.CountAsync(),
                TotalCourses = await _context.Courses.CountAsync(),
                CourseStats = courseStats
            };

            return View(vm);
        }

        public async Task<IActionResult> Users(string? search, string? role)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                users = users.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));

            var list = await users.ToListAsync();

            // فلترة حسب الدور
            if (!string.IsNullOrEmpty(role))
            {
                var filtered = new List<AppUser>();
                foreach (var u in list)
                    if (await _userManager.IsInRoleAsync(u, role))
                        filtered.Add(u);
                list = filtered;
            }

            ViewBag.Search = search;
            ViewBag.Role = role;
            return View(list);
        }

        public async Task<IActionResult> Sessions(string? search)
        {
            var query = _context.Sessions
                .Include(s => s.Course)
                .Include(s => s.CreatedBy)
                .Include(s => s.AttendanceRecords)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.Title.Contains(search) || s.Course.Name.Contains(search));

            var sessions = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
            ViewBag.Search = search;
            return View(sessions);
        }

        public async Task<IActionResult> Logs(int page = 1)
        {
            int pageSize = 20;
            var logs = await _context.AuditLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = Math.Ceiling((double)await _context.AuditLogs.CountAsync() / pageSize);
            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            TempData["Success"] = user.IsActive ? "تم تفعيل الحساب." : "تم تعطيل الحساب.";
            return RedirectToAction("Users");
        }

        // ================================================================
        // Course Management
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> Courses(string? search)
        {
            // التعديل: إضافة Include لـ Sessions و Enrollments
            var query = _context.Courses
                .Include(c => c.Sessions)
                .Include(c => c.Enrollments)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c =>
                    c.Name.Contains(search) ||
                    c.Code.Contains(search));

            ViewBag.Search = search;
            return View(await query
                .OrderBy(c => c.AcademicYear)
                .ThenBy(c => c.Name)
                .ToListAsync());
        }

        [HttpGet]
        public IActionResult CreateCourse() =>
            View(new CourseViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CourseViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // تحقق من تكرار الكود
            bool codeExists = await _context.Courses
                .AnyAsync(c => c.Code == model.Code);

            if (codeExists)
            {
                ModelState.AddModelError("Code", "كود المادة موجود مسبقاً.");
                return View(model);
            }

            _context.Courses.Add(new Course
            {
                Name = model.Name,
                Code = model.Code.ToUpper(),
                AcademicYear = model.AcademicYear,
                CreditHours = model.CreditHours,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"تم إضافة مادة '{model.Name}' بنجاح!";
            return RedirectToAction("Courses");
        }

        [HttpGet]
        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            return View(new CourseViewModel
            {
                Id = course.Id,
                Name = course.Name,
                Code = course.Code,
                AcademicYear = course.AcademicYear,
                CreditHours = course.CreditHours,
                IsActive = course.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(CourseViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var course = await _context.Courses.FindAsync(model.Id);
            if (course == null) return NotFound();

            // تحقق من تكرار الكود (مع استثناء نفس المادة)
            bool codeExists = await _context.Courses
                .AnyAsync(c => c.Code == model.Code && c.Id != model.Id);

            if (codeExists)
            {
                ModelState.AddModelError("Code", "كود المادة مستخدم من مادة أخرى.");
                return View(model);
            }

            course.Name = model.Name;
            course.Code = model.Code.ToUpper();
            course.AcademicYear = model.AcademicYear;
            course.CreditHours = model.CreditHours;
            course.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تحديث المادة بنجاح!";
            return RedirectToAction("Courses");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            course.IsActive = !course.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = course.IsActive
                ? $"تم تفعيل مادة '{course.Name}'."
                : $"تم تعطيل مادة '{course.Name}'.";

            return RedirectToAction("Courses");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Sessions)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            // منع الحذف لو فيه جلسات
            if (course.Sessions.Any())
            {
                TempData["Error"] =
                    "لا يمكن حذف المادة لوجود جلسات مرتبطة بها. " +
                    "قم بتعطيلها بدلاً من الحذف.";
                return RedirectToAction("Courses");
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"تم حذف مادة '{course.Name}' بنجاح.";
            return RedirectToAction("Courses");
        }
    }
}