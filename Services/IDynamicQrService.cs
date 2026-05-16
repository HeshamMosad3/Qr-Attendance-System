namespace QRAttendanceSystem.Services
{
    public interface IDynamicQrService
    {
        Task RefreshSessionQrAsync(int sessionId);
        Task RefreshAllActiveSessionsAsync();
    }
}