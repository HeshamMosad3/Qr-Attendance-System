using System.ComponentModel.DataAnnotations;

namespace QRAttendanceSystem.ViewModels.Account
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور لا تقل عن 6 أحرف")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الجديدة")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [Compare("NewPassword", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}