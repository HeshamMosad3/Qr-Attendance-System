namespace QRAttendanceSystem.Services
{
    public interface IGpsService
    {
        (bool IsValid, string Message) Verify(double lat, double lng);
    }
}