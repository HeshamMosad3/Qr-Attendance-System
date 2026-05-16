namespace QRAttendanceSystem.ViewModels.Admin
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalSessions { get; set; }
        public int TotalAttendanceRecords { get; set; }
        public int TotalCourses { get; set; }
        public List<CourseAttendanceStat> CourseStats { get; set; } = new();
    }

    public class CourseAttendanceStat
    {
        public string CourseName { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public int TotalAttendance { get; set; }
        public double AttendanceRate { get; set; }
    }
}