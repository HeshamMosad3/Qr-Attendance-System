using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using QRAttendanceSystem.Models;
using QRAttendanceSystem.Helpers;
using Microsoft.Extensions.Logging;

namespace QRAttendanceSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        // ===== Core Send Method =====
        private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();

                // استخدام StartTls بشكل صريح لضمان التوافق مع Gmail و Railway
                await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(_settings.Username, _settings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("تم إرسال إيميل إلى {Email} بموضوع: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                // تم تحسين الـ Log لعرض تفاصيل الخطأ بوضوح
                _logger.LogError(ex, "فشل إرسال إيميل إلى {Email}. الخطأ: {Message}", toEmail, ex.Message);
            }
        }

        // ===== Password Reset =====
        public async Task SendPasswordResetAsync(string email, string fullName, string resetLink)
        {
            string subject = "إعادة تعيين كلمة المرور — نظام الحضور QR";
            string html = EmailTemplates.GetPasswordResetTemplate(fullName, resetLink);
            await SendAsync(email, fullName, subject, html);
        }

        // ===== Absence Warning =====
        public async Task SendAbsenceWarningAsync(string email, string studentName, string courseName, double absencePercent)
        {
            string subject = $"⚠️ تحذير غياب — مادة {courseName}";
            string html = EmailTemplates.GetAbsenceWarningTemplate(studentName, courseName, absencePercent);
            await SendAsync(email, studentName, subject, html);
        }

        // ===== Welcome =====
        public async Task SendWelcomeAsync(string email, string fullName)
        {
            string subject = "أهلاً بك في نظام الحضور QR";
            string html = EmailTemplates.GetWelcomeTemplate(fullName);
            await SendAsync(email, fullName, subject, html);
        }

        // ===== Attendance Confirmation =====
        public async Task SendAttendanceConfirmationAsync(string email, string fullName, string sessionTitle, string courseName, DateTime scannedAt)
        {
            string subject = $"✅ تم تسجيل حضورك — {courseName}";
            string html = EmailTemplates.GetAttendanceConfirmTemplate(fullName, sessionTitle, courseName, scannedAt);
            await SendAsync(email, fullName, subject, html);
        }
    }
}