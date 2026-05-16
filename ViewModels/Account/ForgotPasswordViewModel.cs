using System.ComponentModel.DataAnnotations;

namespace QRAttendanceSystem.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "بريد إلكتروني غير صالح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;
    }
}