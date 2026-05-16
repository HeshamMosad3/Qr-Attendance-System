using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Services
{
    public interface IPdfService
    {
        byte[] GenerateAttendanceReport(
            Session session,
            List<AttendanceRecord> records,
            string doctorName);

        byte[] GenerateStudentReport(
            AppUser student,
            List<AttendanceRecord> records);
    }
}