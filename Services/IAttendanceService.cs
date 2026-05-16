using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Services
{
    public interface IAttendanceService
    {
        /// <summary>
        /// تسجيل حضور الطالب باستخدام رمز الـ QR مع التحقق من الموقع الجغرافي اختيارياً
        /// </summary>
        Task<(bool Success, string Message)> RecordAttendanceAsync(
            string token,
            string userId,
            string? ipAddress,
            double? latitude = null,
            double? longitude = null);

        /// <summary>
        /// جلب سجل الحضور الخاص بطالب معين
        /// </summary>
        Task<List<AttendanceRecord>> GetStudentRecordsAsync(string userId);

        /// <summary>
        /// حساب نسبة حضور الطالب في مادة معينة
        /// </summary>
        Task<double> GetStudentAttendancePercentageAsync(string userId, int courseId);

        /// <summary>
        /// التحقق مما إذا كان الطالب قد سجل حضوره بالفعل في هذه الجلسة
        /// </summary>
        Task<bool> IsAlreadyRecordedAsync(string token, string userId);
    }
}