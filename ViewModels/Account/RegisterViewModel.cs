using System.ComponentModel.DataAnnotations;

namespace QRAttendanceSystem.ViewModels.Account
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "بريد إلكتروني غير صالح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور لا تقل عن 6 أحرف")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "الدور مطلوب")]
        [Display(Name = "نوع الحساب")]
        public string Role { get; set; } = "Student";

        [Display(Name = "الرقم الجامعي")]
        public string? StudentId { get; set; }

        [Display(Name = "السنة الدراسية")]
        [Range(1, 4)]
        public int? AcademicYear { get; set; }
    }
}