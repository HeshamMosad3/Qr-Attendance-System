using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore; // ← مهم لاستخدام Include و ToListAsync
using QRAttendanceSystem.Data;       // ← مهم للتعرف على AppDbContext
using QRAttendanceSystem.Models;
using QRAttendanceSystem.Services;

namespace QRAttendanceSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ISessionService _sessionService;
        private readonly IAuditLogService _audit;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpContextAccessor _http;
        private readonly IPdfService _pdfService;
        private readonly IConfiguration _config;
        private readonly AppDbContext _context; // ← تمت إضافة الـ Context هنا

        public AttendanceController(IAttendanceService attendanceService,
            ISessionService sessionService,
            IAuditLogService audit,
            UserManager<AppUser> userManager,
            IHttpContextAccessor http,
            IPdfService pdfService,
            IConfiguration config,
            AppDbContext context) // ← حقن الـ Context هنا
        {
            _attendanceService = attendanceService;
            _sessionService = sessionService;
            _audit = audit;
            _userManager = userManager;
            _http = http;
            _pdfService = pdfService;
            _config = config;
            _context = context; // ← تعيين الـ Context
        }

        // GET: /Attendance/Attend?token=xxx
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Attend(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.IsValid = false;
                ViewBag.Message = "رمز QR مفقود أو غير صالح.";
                return View();
            }

            var session = await _sessionService.GetSessionByTokenAsync(token);

            if (session == null)
            {
                ViewBag.IsValid = false;
                ViewBag.Message = "رمز QR غير صالح.";
                return View();
            }

            if (!session.IsActive)
            {
                ViewBag.IsValid = false;
                ViewBag.Message = "هذه الجلسة غير نشطة.";
                return View();
            }

            if (DateTime.UtcNow > session.QrExpiresAt)
            {
                ViewBag.IsValid = false;
                ViewBag.Message = "انتهت صلاحية رمز QR. تواصل مع الدكتور لتجديده.";
                return View();
            }

            var userId = _userManager.GetUserId(User)!;
            bool already = await _attendanceService.IsAlreadyRecordedAsync(token, userId);

            if (already)
            {
                ViewBag.IsValid = false;
                ViewBag.Message = "تم تسجيل حضورك مسبقاً في هذه الجلسة.";
                return View();
            }

            // GPS Config
            bool gpsEnabled = _config.GetValue<bool>("AppSettings:GpsVerificationEnabled", false);

            ViewBag.IsValid = true;
            ViewBag.Token = token;
            ViewBag.SessionTitle = session.Title;
            ViewBag.CourseName = session.Course?.Name;
            ViewBag.SessionType = session.Type.ToString();

            ViewBag.GpsEnabled = gpsEnabled;
            ViewBag.CollegeLat = _config.GetValue<double>("AppSettings:CollegeLat", 30.0444);
            ViewBag.CollegeLng = _config.GetValue<double>("AppSettings:CollegeLng", 31.2357);
            ViewBag.GpsRadius = _config.GetValue<double>("AppSettings:GpsRadiusMeters", 500);

            return View();
        }

        [Authorize(Roles = "Student")]
        public IActionResult Scanner() => View();

        // POST: /Attendance/ConfirmAttend
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        [EnableRateLimiting("attendance")]
        public async Task<IActionResult> ConfirmAttend(
            string token,
            double? latitude = null,
            double? longitude = null)
        {
            var userId = _userManager.GetUserId(User)!;
            var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

            var (success, message) = await _attendanceService.RecordAttendanceAsync(
                token, userId, ip, latitude, longitude);

            if (success)
            {
                var session = await _sessionService.GetSessionByTokenAsync(token);
                await _audit.LogAsync(
                    "RecordAttendance", userId,
                    $"حضور في: {session?.Title} | GPS: {latitude},{longitude}", ip);
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction("MyRecords");
        }

        // GET: /Attendance/MyRecords
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyRecords()
        {
            var userId = _userManager.GetUserId(User)!;
            var records = await _attendanceService.GetStudentRecordsAsync(userId);

            // ✅ جلب المواد المسجل فيها
            var enrolledCourses = await _context.Enrollments
                .Where(e => e.UserId == userId)
                .Include(e => e.Course)
                .Select(e => e.Course)
                .ToListAsync();

            // نسبة حضور لكل مادة
            var courseStats = records
                .GroupBy(r => new { r.Session?.CourseId, r.Session?.Course?.Name })
                .Select(g => new
                {
                    CourseName = g.Key.Name ?? "غير محدد",
                    Attended = g.Count(),
                    OnTime = g.Count(r => r.Status == AttendanceStatus.OnTime),
                    Late = g.Count(r => r.Status == AttendanceStatus.Late)
                }).ToList();

            ViewBag.CourseStats = courseStats;
            ViewBag.EnrolledCourses = enrolledCourses; // ← تمرير المواد للـ View

            return View(records);
        }

        // Action الخاص بتصدير الـ PDF للطالب
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ExportMyPdf()
        {
            var userId = _userManager.GetUserId(User)!;
            var user = await _userManager.GetUserAsync(User);
            var records = await _attendanceService
                .GetStudentRecordsAsync(userId);

            var bytes = _pdfService.GenerateStudentReport(
                user!, records);

            return File(bytes, "application/pdf",
                $"MyAttendance_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}