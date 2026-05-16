using System.ComponentModel.DataAnnotations;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.ViewModels.Session
{
    public class CreateSessionViewModel
    {
        [Required(ErrorMessage = "عنوان الجلسة مطلوب")]
        [Display(Name = "عنوان الجلسة")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "نوع الجلسة")]
        public SessionType Type { get; set; }

        [Required(ErrorMessage = "المادة مطلوبة")]
        [Display(Name = "المادة")]
        public int CourseId { get; set; }

        [Required]
        [Display(Name = "تاريخ الجلسة")]
        [DataType(DataType.DateTime)]
        public DateTime SessionDate { get; set; } = DateTime.Now;

        [Display(Name = "حد التأخير (دقيقة)")]
        [Range(1, 120)]
        public int LateLimitMinutes { get; set; } = 15;
    }
}