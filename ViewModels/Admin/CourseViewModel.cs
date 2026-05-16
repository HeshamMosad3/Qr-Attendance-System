using System.ComponentModel.DataAnnotations;

namespace QRAttendanceSystem.ViewModels.Admin
{
    public class CourseViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المادة مطلوب")]
        [Display(Name = "اسم المادة")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "كود المادة مطلوب")]
        [Display(Name = "كود المادة")]
        [StringLength(20, ErrorMessage = "الكود لا يزيد عن 20 حرف")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "السنة الدراسية مطلوبة")]
        [Display(Name = "السنة الدراسية")]
        [Range(1, 4, ErrorMessage = "السنة من 1 إلى 4")]
        public int AcademicYear { get; set; } = 1;

        [Required(ErrorMessage = "عدد الساعات مطلوب")]
        [Display(Name = "الساعات المعتمدة")]
        [Range(1, 6, ErrorMessage = "الساعات من 1 إلى 6")]
        public int CreditHours { get; set; } = 3;

        [Display(Name = "نشطة")]
        public bool IsActive { get; set; } = true;
    }
}