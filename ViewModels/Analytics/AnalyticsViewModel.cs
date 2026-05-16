namespace QRAttendanceSystem.ViewModels.Analytics
{
    public class AnalyticsViewModel
    {
        public string CourseName { get; set; } = "";
        public int CourseId { get; set; }
        public int TotalSessions { get; set; }
        public int TotalAttendance { get; set; }
        public double OverallAttendanceRate { get; set; }
        public List<SessionTrend> SessionTrends { get; set; } = new();
        public List<TopAbsentStudent> TopAbsent { get; set; } = new();
        public List<DayHeatmap> WeeklyHeatmap { get; set; } = new();
        public List<HourlyData> HourlyDistribution { get; set; } = new();
    }

    public class SessionTrend
    {
        public string Label { get; set; } = "";
        public DateTime Date { get; set; }
        public int AttendanceCount { get; set; }
        public double AttendanceRate { get; set; }
    }

    public class TopAbsentStudent
    {
        public string Name { get; set; } = "";
        public string? StudentId { get; set; }
        public int TotalSessions { get; set; }
        public int Attended { get; set; }
        public double AbsenceRate { get; set; }
    }

    public class DayHeatmap
    {
        public string DayName { get; set; } = "";
        public int DayOfWeek { get; set; }
        public List<int> HourlyCounts { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class HourlyData
    {
        public int Hour { get; set; }
        public int Count { get; set; }
        public string Label => $"{Hour:D2}:00";
    }
}