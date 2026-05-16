using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string action, string? userId, string? details = null, string? ipAddress = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                UserId = userId,
                Details = details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}