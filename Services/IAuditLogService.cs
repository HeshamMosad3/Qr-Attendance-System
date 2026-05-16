namespace QRAttendanceSystem.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string action, string? userId, string? details = null, string? ipAddress = null);
    }
}