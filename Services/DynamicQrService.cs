using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Hubs;

namespace QRAttendanceSystem.Services
{
    public class DynamicQrService : IDynamicQrService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<AttendanceHub> _hubContext;
        private readonly ILogger<DynamicQrService> _logger;

        public DynamicQrService(
            IServiceScopeFactory scopeFactory,
            IHubContext<AttendanceHub> hubContext,
            ILogger<DynamicQrService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task RefreshSessionQrAsync(int sessionId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var qrService = scope.ServiceProvider.GetRequiredService<IQrCodeService>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var session = await context.Sessions
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive);

            if (session == null) return;

            var newToken = qrService.GenerateQrToken();
            var expiryMinutes = config.GetValue<int>("AppSettings:QrExpiryMinutes", 30);

            session.QrToken = newToken;

            // 1. تعديل التوقيت لـ Now بدلاً من UtcNow ليتطابق مع فحص الحضور
            session.QrExpiresAt = DateTime.Now.AddMinutes(expiryMinutes);

            await context.SaveChangesAsync();

            // 2. إجبار السيرفر على وضع الرابط كاملاً (Absolute URL) داخل الـ QR Code
            var baseUrl = config.GetValue<string>("AppSettings:BaseUrl")?.TrimEnd('/');
            if (string.IsNullOrEmpty(baseUrl) || baseUrl.Contains("runasp.net"))
            {
                // استخدام رابط Railway كضمان لو الرابط القديم لسه في الإعدادات
                baseUrl = "https://qr-attendance-system-production-bc1e.up.railway.app";
            }

            var attendUrl = $"{baseUrl}/Attendance/Attend?token={newToken}";

            // توليد صورة QR جديدة بالرابط الكامل
            var qrBase64 = qrService.GenerateQrCodeBase64(attendUrl);

            // إرسال للدكتور فوراً عن طريق SignalR
            await _hubContext.Clients
                .Group($"session_{sessionId}")
                .SendAsync("QrRefreshed", new
                {
                    token = newToken,
                    qrBase64 = qrBase64,
                    attendUrl = attendUrl,
                    expiresAt = session.QrExpiresAt.ToString("o"),
                    refreshedAt = DateTime.Now.ToString("HH:mm:ss") // تعديل لـ Now
                });

            _logger.LogInformation(
                "تم تجديد QR للجلسة {SessionId} — Token: {Token}",
                sessionId, newToken[..8] + "...");
        }

        public async Task RefreshAllActiveSessionsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            // جلب كل الجلسات النشطة اللي لسه مش منتهية
            var activeSessions = await context.Sessions
                .Where(s => s.IsActive
                    && s.QrExpiresAt > DateTime.UtcNow)
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var sessionId in activeSessions)
            {
                await RefreshSessionQrAsync(sessionId);
                // delay صغير بين كل session عشان منحملش السيرفر
                await Task.Delay(100);
            }

            if (activeSessions.Count > 0)
                _logger.LogInformation(
                    "تم تجديد QR لـ {Count} جلسة نشطة",
                    activeSessions.Count);
        }
    }
}