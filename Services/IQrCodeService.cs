namespace QRAttendanceSystem.Services
{
    public interface IQrCodeService
    {
        string GenerateQrToken();
        string GenerateQrCodeBase64(string url);
    }
}