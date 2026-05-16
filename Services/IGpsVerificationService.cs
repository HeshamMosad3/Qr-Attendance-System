namespace QRAttendanceSystem.Services
{
    public class GpsVerificationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = "";
        public double? DistanceMeters { get; set; }
    }

    public interface IGpsVerificationService
    {
        GpsVerificationResult Verify(
            double studentLat, double studentLng);
        double CalculateDistance(
            double lat1, double lng1,
            double lat2, double lng2);
    }
}