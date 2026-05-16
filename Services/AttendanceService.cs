using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Hubs;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IHubContext<AttendanceHub> _hubContext;
        private readonly IGpsVerificationService _gps;
        public AttendanceService(
            AppDbContext context,
            IConfiguration config,
            IEmailService emailService,
            IHubContext<AttendanceHub> hubContext,
           IGpsVerificationService gps) // حقن الخدمة هنا
        {
            _context = context;
            _config = config;
            _emailService = emailService;
            _hubContext = hubContext;
            _gps = gps;
        }

        public async Task<(bool Success, string Message)>
            RecordAttendanceAsync(
                string token,
                string userId,
                string? ipAddress,
                double? latitude = null,
                double? longitude = null)
        {
            var session = await _context.Sessions
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.QrToken == token);

            if (session == null)
                return (false, "رمز QR غير صالح.");

            if (!session.IsActive)
                return (false, "هذه الجلسة غير نشطة.");

            if (DateTime.UtcNow > session.QrExpiresAt)
                return (false,
                    "انتهت صلاحية رمز QR. " +
                    "تواصل مع الدكتور لتجديده.");

            bool already = await _context.AttendanceRecords
                .AnyAsync(a => a.UserId == userId
                            && a.SessionId == session.Id);
            if (already)
                return (false,
                    "تم تسجيل حضورك مسبقاً في هذه الجلسة.");

            // ===== GPS Verification =====
            bool gpsEnabled = _config.GetValue<bool>(
                "AppSettings:GpsVerificationEnabled", false);

            if (gpsEnabled)
            {
                if (!latitude.HasValue || !longitude.HasValue)
                    return (false,
                        "يجب السماح بالوصول للموقع لتسجيل الحضور.");

                var gpsResult = _gps.Verify(
                    latitude.Value, longitude.Value);

                if (!gpsResult.IsValid)
                    return (false, gpsResult.Message);
            }

            // تحديد OnTime / Late
            var lateLimit = session.CreatedAt
                .AddMinutes(session.LateLimitMinutes);
            var status = DateTime.UtcNow <= lateLimit
                ? AttendanceStatus.OnTime
                : AttendanceStatus.Late;

            var record = new AttendanceRecord
            {
                UserId = userId,
                SessionId = session.Id,
                ScannedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                Status = status
            };

            _context.AttendanceRecords.Add(record);
            await _context.SaveChangesAsync();

            // جلب بيانات الطالب للإرسال عبر SignalR
            var student = await _context.Users.FindAsync(userId);

            // حساب الأعداد الجديدة
            var allRecords = await _context.AttendanceRecords
                .Where(a => a.SessionId == session.Id)
                .ToListAsync();

            int onTimeCount = allRecords
                .Count(a => a.Status == AttendanceStatus.OnTime);
            int lateCount = allRecords
                .Count(a => a.Status == AttendanceStatus.Late);

            // إرسال للدكتور عبر SignalR فوراً
            await _hubContext.Clients
                .Group($"session_{session.Id}")
                .SendAsync("NewAttendance", new
                {
                    studentName = student?.FullName ?? "طالب",
                    studentId = student?.StudentId ?? "-",
                    scannedAt = record.ScannedAt
                        .ToString("HH:mm:ss"),
                    status = status.ToString(),
                    onTimeCount = onTimeCount,
                    lateCount = lateCount,
                    totalCount = allRecords.Count
                });

            // تحقق من نسبة الغياب
            await CheckAbsenceWarningAsync(userId, session.CourseId);

            string statusMsg = status == AttendanceStatus.OnTime
                ? "في الوقت" : "متأخر";
            return (true,
                $"تم تسجيل حضورك بنجاح ({statusMsg}).");
        }

        public async Task<bool> IsAlreadyRecordedAsync(
            string token, string userId)
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.QrToken == token);
            if (session == null) return false;

            return await _context.AttendanceRecords
                .AnyAsync(a => a.UserId == userId
                            && a.SessionId == session.Id);
        }

        public async Task<List<AttendanceRecord>>
            GetStudentRecordsAsync(string userId)
        {
            return await _context.AttendanceRecords
                .Include(a => a.Session)
                    .ThenInclude(s => s.Course)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.ScannedAt)
                .ToListAsync();
        }

        public async Task<double>
            GetStudentAttendancePercentageAsync(
                string userId, int courseId)
        {
            int total = await _context.Sessions
                .CountAsync(s => s.CourseId == courseId);
            if (total == 0) return 100;

            int attended = await _context.AttendanceRecords
                .CountAsync(a => a.UserId == userId
                    && a.Session.CourseId == courseId);

            return Math.Round(
                (double)attended / total * 100, 1);
        }

        private async Task CheckAbsenceWarningAsync(
            string userId, int courseId)
        {
            int warnPct = _config.GetValue<int>(
                "AppSettings:AbsenceWarningPercent", 25);
            int total = await _context.Sessions
                .CountAsync(s => s.CourseId == courseId);
            if (total == 0) return;

            int attended = await _context.AttendanceRecords
                .CountAsync(a => a.UserId == userId
                    && a.Session.CourseId == courseId);

            double absencePct =
                (double)(total - attended) / total * 100;

            if (absencePct >= warnPct)
            {
                var user = await _context.Users.FindAsync(userId);
                var course = await _context.Courses
                    .FindAsync(courseId);

                if (user?.Email != null && course != null)
                    await _emailService.SendAbsenceWarningAsync(
                        user.Email, user.FullName,
                        course.Name, absencePct);
            }
        }
    }
}