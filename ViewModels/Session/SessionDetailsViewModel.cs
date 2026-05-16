using QRAttendanceSystem.Models;

using SessionModel = QRAttendanceSystem.Models.Session;

namespace QRAttendanceSystem.ViewModels.Session
{
    public class SessionDetailsViewModel
    {
        public SessionModel Session { get; set; }
        public string QrCodeBase64 { get; set; } = string.Empty;
        public string AttendUrl { get; set; } = string.Empty;
        public int SecondsRemaining { get; set; }
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
        public int OnTimeCount => AttendanceRecords.Count(a => a.Status == AttendanceStatus.OnTime);
        public int LateCount => AttendanceRecords.Count(a => a.Status == AttendanceStatus.Late);
    }
}