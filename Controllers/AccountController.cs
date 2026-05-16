using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using QRAttendanceSystem.Models;
using QRAttendanceSystem.Services;
using QRAttendanceSystem.ViewModels.Account;
using System.Text;
using System.Text.Encodings.Web;

namespace QRAttendanceSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _audit;
        private readonly IHttpContextAccessor _http;
        private readonly IConfiguration _config;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IEmailService emailService,
            IAuditLogService audit,
            IHttpContextAccessor http,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _audit = audit;
            _http = http;
            _config = config;
        }

        // ============================================================
        // Register
        // ============================================================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.Role == "Admin")
            {
                ModelState.AddModelError("", "لا يمكن التسجيل كأدمن.");
                return View(model);
            }

            var user = new AppUser
            {
                FullName = model.FullName,
                UserName = model.Email,
                Email = model.Email,
                StudentId = model.StudentId,
                AcademicYear = model.AcademicYear,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                await _signInManager.SignInAsync(user, false);

                // Welcome email في الخلفية
                _ = _emailService.SendWelcomeAsync(user.Email!, user.FullName);

                await _audit.LogAsync("Register", user.Id,
                    $"تسجيل حساب جديد — دور: {model.Role}",
                    _http.HttpContext?.Connection.RemoteIpAddress?.ToString());

                TempData["Success"] = "تم إنشاء حسابك بنجاح! أهلاً بك.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);

            return View(model);
        }

        // ============================================================
        // Login
        // ============================================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(
            LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password,
                model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                await _audit.LogAsync("Login", user?.Id, "تسجيل دخول",
                    _http.HttpContext?.Connection.RemoteIpAddress?.ToString());

                TempData["Success"] = $"أهلاً {user?.FullName}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
                ModelState.AddModelError("",
                    "الحساب مقفل مؤقتاً. حاول مرة أخرى بعد 15 دقيقة.");
            else
                ModelState.AddModelError("",
                    "البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            return View(model);
        }

        // ============================================================
        // Logout
        // ============================================================
        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Info"] = "تم تسجيل الخروج بنجاح.";
            return RedirectToAction("Login");
        }

        // ============================================================
        // Forgot Password — الخطوة 1: المستخدم يدخل الإيميل
        // ============================================================
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost, ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // دايماً نعرض نفس الرسالة سواء الإيميل موجود أو لا
            // عشان منفشش إن الإيميل مسجل في النظام
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                // توليد token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // encode التوكن عشان يتبعت في الـ URL بأمان
                var encodedToken = WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(token));

                // بناء الرابط
                var resetLink = Url.Action(
                    "ResetPassword", "Account",
                    new { token = encodedToken, email = model.Email },
                    Request.Scheme)!;

                // إرسال الإيميل
                await _emailService.SendPasswordResetAsync(
                    user.Email!, user.FullName, resetLink);

                await _audit.LogAsync("ForgotPassword", user.Id,
                    "طلب إعادة تعيين كلمة المرور",
                    _http.HttpContext?.Connection.RemoteIpAddress?.ToString());
            }

            // نوجه للصفحة دي بغض النظر
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        // ============================================================
        // Reset Password — الخطوة 2: المستخدم يكتب كلمة المرور الجديدة
        // ============================================================
        [HttpGet]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (token == null || email == null)
            {
                TempData["Error"] = "رابط إعادة التعيين غير صالح.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // نعرض صفحة نجاح حتى لو المستخدم مش موجود (security)
                return RedirectToAction("ResetPasswordConfirmation");
            }

            // decode التوكن
            var decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(model.Token));

            var result = await _userManager.ResetPasswordAsync(
                user, decodedToken, model.NewPassword);

            if (result.Succeeded)
            {
                await _audit.LogAsync("ResetPassword", user.Id,
                    "تم إعادة تعيين كلمة المرور بنجاح",
                    _http.HttpContext?.Connection.RemoteIpAddress?.ToString());

                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        // ============================================================
        // Change Password — المستخدم مسجل الدخول ويغير كلمة المرور
        // ============================================================
        [HttpGet, Authorize]
        public IActionResult ChangePassword() => View();

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(
                user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // تحديث الـ security stamp عشان الـ cookie يتجدد
                await _signInManager.RefreshSignInAsync(user);

                await _audit.LogAsync("ChangePassword", user.Id,
                    "تم تغيير كلمة المرور",
                    _http.HttpContext?.Connection.RemoteIpAddress?.ToString());

                TempData["Success"] = "تم تغيير كلمة المرور بنجاح!";
                return RedirectToAction("ChangePassword");
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);

            return View(model);
        }

        public IActionResult AccessDenied() => View();
    }
}