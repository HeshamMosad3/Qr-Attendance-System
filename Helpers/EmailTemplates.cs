namespace QRAttendanceSystem.Helpers
{
    public static class EmailTemplates
    {
        private static string BaseTemplate(string content) => $@"
<!DOCTYPE html>
<html lang='ar' dir='rtl'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<style>
  *{{margin:0;padding:0;box-sizing:border-box;}}
  body{{font-family:'Segoe UI',Tahoma,Arial,sans-serif;background:#f0f2f5;direction:rtl;}}
  .wrapper{{max-width:600px;margin:30px auto;background:#fff;border-radius:16px;
            overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.10);}}
  .header{{background:linear-gradient(135deg,#1e3a5f,#2d6a9f);
           padding:32px 40px;text-align:center;color:#fff;}}
  .header h1{{font-size:22px;font-weight:700;margin-bottom:4px;}}
  .header p{{font-size:13px;opacity:0.8;}}
  .body{{padding:32px 40px;}}
  .greeting{{font-size:18px;font-weight:600;color:#1e3a5f;margin-bottom:16px;}}
  .text{{font-size:14px;color:#444;line-height:1.8;margin-bottom:16px;}}
  .btn{{display:inline-block;background:linear-gradient(135deg,#1e3a5f,#2d6a9f);
        color:#fff;padding:14px 32px;border-radius:10px;text-decoration:none;
        font-size:15px;font-weight:600;margin:20px 0;}}
  .info-box{{background:#f0f7ff;border-right:4px solid #2d6a9f;
             border-radius:8px;padding:16px;margin:20px 0;}}
  .info-box p{{font-size:13px;color:#444;margin:4px 0;}}
  .warning-box{{background:#fff8f0;border-right:4px solid #fd7e14;
                border-radius:8px;padding:16px;margin:20px 0;}}
  .success-box{{background:#f0fdf4;border-right:4px solid #28a745;
                border-radius:8px;padding:16px;margin:20px 0;}}
  .footer{{background:#f8f9fa;padding:20px 40px;text-align:center;
           font-size:12px;color:#888;border-top:1px solid #eee;}}
  .footer a{{color:#2d6a9f;text-decoration:none;}}
  .divider{{height:1px;background:#eee;margin:24px 0;}}
  .stat{{display:inline-block;background:#e8f4fd;padding:8px 16px;
         border-radius:8px;margin:4px;font-size:13px;color:#1e3a5f;font-weight:600;}}
</style>
</head>
<body>
<div class='wrapper'>
  <div class='header'>
    <h1>🎓 نظام الحضور QR</h1>
    <p>Faculty of Business Administration — EEIU</p>
  </div>
  <div class='body'>{content}</div>
  <div class='footer'>
    <p>هذا البريد تم إرساله تلقائياً من نظام الحضور. لا ترد على هذه الرسالة.</p>
    <p style='margin-top:6px;'>
      © 2025 Faculty of Business Administration — EEIU
    </p>
  </div>
</div>
</body>
</html>";

        public static string GetPasswordResetTemplate(string fullName, string resetLink) =>
            BaseTemplate($@"
<p class='greeting'>مرحباً {fullName} 👋</p>
<p class='text'>
  تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك في نظام الحضور QR.
</p>
<div style='text-align:center;'>
  <a href='{resetLink}' class='btn'>🔑 إعادة تعيين كلمة المرور</a>
</div>
<div class='info-box'>
  <p>⏰ <strong>مهم:</strong> هذا الرابط صالح لمدة <strong>24 ساعة</strong> فقط</p>
  <p>🔒 الرابط لا يمكن استخدامه إلا مرة واحدة</p>
</div>
<p class='text'>
  إذا لم تطلب إعادة تعيين كلمة المرور، يمكنك تجاهل هذا البريد بأمان.
  حسابك آمن ولم يتم تغيير أي شيء.
</p>
<div class='divider'></div>
<p style='font-size:12px;color:#888;'>
  إذا لم يعمل الزر، انسخ الرابط التالي في متصفحك:<br>
  <a href='{resetLink}' 
     style='color:#2d6a9f;word-break:break-all;font-size:11px;'>
    {resetLink}
  </a>
</p>");

        public static string GetAbsenceWarningTemplate(string studentName, string courseName, double absencePercent) =>
            BaseTemplate($@"
<p class='greeting'>تحذير هام — {studentName}</p>
<div class='warning-box'>
  <p>⚠️ وصلت نسبة غيابك في مادة <strong>{courseName}</strong> إلى:</p>
  <p style='font-size:28px;font-weight:700;color:#fd7e14;
            text-align:center;margin:12px 0;'>
    {absencePercent:F1}%
  </p>
</div>
<p class='text'>
  وفقاً للوائح الجامعة، إذا تجاوزت نسبة الغياب <strong>25%</strong>
  قد لا تُسمح لك بدخول الامتحان في هذه المادة.
</p>
<p class='text'>
  يُرجى الانتظام في الحضور والتواصل مع الدكتور المسؤول عن المادة.
</p>
<div class='divider'></div>
<p style='font-size:13px;color:#888;'>
  تم إرسال هذا التحذير تلقائياً من نظام الحضور.
</p>");

        public static string GetWelcomeTemplate(string fullName) =>
            BaseTemplate($@"
<p class='greeting'>أهلاً وسهلاً {fullName}! 🎉</p>
<p class='text'>
  يسعدنا انضمامك إلى نظام الحضور الذكي بالـ QR Code.
  يمكنك الآن تسجيل حضورك بسهولة عن طريق مسح رمز QR في بداية كل محاضرة.
</p>
<div class='success-box'>
  <p>✅ تم إنشاء حسابك بنجاح</p>
  <p>📱 امسح رمز QR في بداية كل محاضرة</p>
  <p>📊 تابع سجل حضورك من حسابك</p>
</div>
<p class='text'>في حالة وجود أي استفسار، تواصل مع إدارة الكلية.</p>");

        public static string GetAttendanceConfirmTemplate(string fullName, string sessionTitle, string courseName, DateTime scannedAt) =>
            BaseTemplate($@"
<p class='greeting'>تم تسجيل حضورك ✅</p>
<div class='success-box'>
  <p>✅ تم تسجيل حضورك بنجاح في:</p>
</div>
<div class='info-box'>
  <p>📚 <strong>المادة:</strong> {courseName}</p>
  <p>📖 <strong>الجلسة:</strong> {sessionTitle}</p>
  <p>🕐 <strong>وقت التسجيل:</strong>
     {scannedAt.ToString("dd/MM/yyyy — HH:mm")}</p>
</div>
<p class='text'>
  يمكنك مراجعة سجل حضورك الكامل من خلال حسابك في النظام.
</p>");
    }
}