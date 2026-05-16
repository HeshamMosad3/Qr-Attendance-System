namespace QRAttendanceSystem.Services
{
    public interface IEmailService
    {
        Task SendAbsenceWarningAsync(string email, string studentName,
            string courseName, double absencePercent);

        Task SendPasswordResetAsync(string email, string fullName,
            string resetLink);

        Task SendWelcomeAsync(string email, string fullName);

        Task SendAttendanceConfirmationAsync(string email, string fullName,
            string sessionTitle, string courseName, DateTime scannedAt);
    }
}