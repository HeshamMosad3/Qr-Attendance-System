using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Helpers;
using QRAttendanceSystem.Models;
using QRAttendanceSystem.Services;
using QRAttendanceSystem.ViewModels.Session;

namespace QRAttendanceSystem.Controllers
{
    [Authorize(Roles = "Doctor,Admin")]
    public class SessionController : Controller
    {
        private readonly ISessionService _sessionService;
        private readonly IQrCodeService _qrService;
        private readonly IAuditLogService _audit;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _http;
        private readonly IDynamicQrService _dynamicQrService;
        private readonly IPdfService _pdfService; 

        public SessionController(ISessionService sessionService, IQrCodeService qrService,
            IAuditLogService audit, UserManager<AppUser> userManager,
            AppDbContext context, IHttpContextAccessor http,
            IDynamicQrService dynamicQrService, IPdfService pdfService)
        {
            _sessionService = sessionService;
            _qrService = qrService;
            _audit = audit;
            _userManager = userManager;
            _context = context;
            _http = http;
            _dynamicQrService = dynamicQrService;
            _pdfService = pdfService; // ← حقن الخدمة
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var sessions = await _sessionService.GetDoctorSessionsAsync(userId);
            return View(sessions);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Courses = await _context.Courses.Where(c => c.IsActive).ToListAsync();
            return View(new CreateSessionViewModel { SessionDate = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSessionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Courses = await _context.Courses.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;
            var session = await _sessionService.CreateSessionAsync(model, userId);
            await _audit.LogAsync("CreateSession", userId, $"إنشاء جلسة: {model.Title}",
                _http.HttpContext?.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "تم إنشاء الجلسة بنجاح!";
            return RedirectToAction("Details", new { id = session.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var session = await _sessionService.GetSessionByIdAsync(id);
            if (session == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (session.CreatedByUserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var attendUrl = $"{Request.Scheme}://{Request.Host}/Attendance/Attend?token={session.QrToken}";


            var vm = new SessionDetailsViewModel
            {
                Session = session,
                QrCodeBase64 = _qrService.GenerateQrCodeBase64(attendUrl),
                AttendUrl = attendUrl,
                SecondsRemaining = Math.Max(0,
                    (int)(session.QrExpiresAt - DateTime.UtcNow).TotalSeconds),
                AttendanceRecords = session.AttendanceRecords.ToList()
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualRefreshQr(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var session = await _sessionService.GetSessionByIdAsync(id);

            if (session == null || (session.CreatedByUserId != userId && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            await _dynamicQrService.RefreshSessionQrAsync(id);
            await _audit.LogAsync("ManualRefreshQr", userId, $"تجديد يدوي للـ QR — جلسة {id}");

            return Ok(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshQr(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var newToken = await _sessionService.RefreshQrAsync(id, userId);

            if (string.IsNullOrEmpty(newToken))
            {
                TempData["Error"] = "فشل تجديد الـ QR.";
                return RedirectToAction("Details", new { id });
            }

            await _audit.LogAsync("RefreshQr", userId, $"تجديد QR للجلسة رقم {id}");
            TempData["Success"] = "تم تجديد رمز QR بنجاح!";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var success = await _sessionService.DeactivateSessionAsync(id, userId);

            if (!success)
            {
                TempData["Error"] = "فشل إيقاف الجلسة.";
                return RedirectToAction("Details", new { id });
            }

            await _audit.LogAsync("DeactivateSession", userId, $"إيقاف الجلسة رقم {id}");
            TempData["Info"] = "تم إيقاف الجلسة.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Report(int id)
        {
            var session = await _sessionService.GetSessionByIdAsync(id);
            if (session == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (session.CreatedByUserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            return View(session);
        }

        public async Task<IActionResult> ExportExcel(int id)
        {
            var session = await _sessionService.GetSessionByIdAsync(id);
            if (session == null) return NotFound();

            var records = await _sessionService.GetSessionAttendanceAsync(id);
            var bytes = ExcelHelper.GenerateAttendanceReport(records, session.Title);

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Attendance_{session.Title}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ==========================================
        // Action الخاص بتصدير الـ PDF
        // ==========================================
        public async Task<IActionResult> ExportPdf(int id)
        {
            var session = await _sessionService.GetSessionByIdAsync(id);
            if (session == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (session.CreatedByUserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var records = await _sessionService.GetSessionAttendanceAsync(id);
            var user = await _userManager.GetUserAsync(User);

            var bytes = _pdfService.GenerateAttendanceReport(
                session, records, user?.FullName ?? "");

            return File(bytes, "application/pdf",
                $"Attendance_{session.Title}_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}