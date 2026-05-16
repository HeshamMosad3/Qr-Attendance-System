namespace QRAttendanceSystem.Services
{
    public class GpsVerificationService
        : IGpsVerificationService
    {
        private readonly IConfiguration _config;

        // إحداثيات الكلية — غيّرها لإحداثيات حقيقية
        private double CollegeLat =>
            _config.GetValue<double>(
                "AppSettings:CollegeLat", 30.0444);
        private double CollegeLng =>
            _config.GetValue<double>(
                "AppSettings:CollegeLng", 31.2357);
        private double AllowedRadiusMeters =>
            _config.GetValue<double>(
                "AppSettings:GpsRadiusMeters", 500);

        public GpsVerificationService(
            IConfiguration config)
        {
            _config = config;
        }

        public GpsVerificationResult Verify(
            double studentLat, double studentLng)
        {
            // تحقق من صحة الإحداثيات
            if (studentLat == 0 && studentLng == 0)
                return new GpsVerificationResult
                {
                    IsValid = false,
                    Message = "لم يتم الحصول على الموقع."
                };

            double distance = CalculateDistance(
                studentLat, studentLng,
                CollegeLat, CollegeLng);

            bool isValid = distance <= AllowedRadiusMeters;

            return new GpsVerificationResult
            {
                IsValid = isValid,
                DistanceMeters = Math.Round(distance),
                Message = isValid
                    ? $"أنت داخل نطاق الكلية " +
                      $"({Math.Round(distance)} متر)"
                    : $"أنت خارج نطاق الكلية " +
                      $"({Math.Round(distance)} متر). " +
                      $"يجب أن تكون على بُعد أقل من " +
                      $"{AllowedRadiusMeters} متر."
            };
        }

        // Haversine Formula
        public double CalculateDistance(
            double lat1, double lng1,
            double lat2, double lng2)
        {
            const double R = 6371000; // نصف قطر الأرض بالمتر
            double dLat = ToRad(lat2 - lat1);
            double dLng = ToRad(lng2 - lng1);

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) *
                Math.Cos(ToRad(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(
                Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private static double ToRad(double deg)
            => deg * Math.PI / 180;
    }
}