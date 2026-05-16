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
            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();
            var qrService = scope.ServiceProvider
                .GetRequiredService<IQrCodeService>();
            var config = scope.ServiceProvider
                .GetRequiredService<IConfiguration>();

            var session = await context.Sessions
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == sessionId
                    && s.IsActive);

            if (session == null) return;

            // توليد token جديد
            var newToken = qrService.GenerateQrToken();
            var expiryMinutes = config.GetValue<int>(
                "AppSettings:QrExpiryMinutes", 30);

            session.QrToken = newToken;
            session.QrExpiresAt = DateTime.UtcNow
                .AddMinutes(expiryMinutes);

            await context.SaveChangesAsync();

            // بناء رابط الحضور الجديد
            var attendUrl = $"/Attendance/Attend?token={newToken}";

            // توليد صورة QR جديدة
            // الرابط الكامل بيتبني في الـ Background Service
            var qrBase64 = qrService.GenerateQrCodeBase64(attendUrl);

            // إرسال للدكتور فوراً عن طريق SignalR
            await _hubContext.Clients
                .Group($"session_{sessionId}")
                .SendAsync("QrRefreshed", new
                {
                    token = newToken,
                    qrBase64 = qrBase64,
                    attendUrl = attendUrl,
                    expiresAt = session.QrExpiresAt
                        .ToString("o"), // ISO 8601
                    refreshedAt = DateTime.UtcNow
                        .ToString("HH:mm:ss")
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