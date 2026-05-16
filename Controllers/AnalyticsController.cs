using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;
using QRAttendanceSystem.ViewModels.Analytics;

namespace QRAttendanceSystem.Controllers
{
    [Authorize(Roles = "Doctor,Admin")]
    public class AnalyticsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AnalyticsController(
            AppDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            int? courseId)
        {
            var userId = _userManager.GetUserId(User)!;

            // جلب المواد المتاحة للدكتور
            var courses = await _context.Sessions
                .Where(s => s.CreatedByUserId == userId
                    || User.IsInRole("Admin"))
                .Include(s => s.Course)
                .Select(s => s.Course)
                .Distinct()
                .ToListAsync();

            ViewBag.Courses = courses;

            if (!courseId.HasValue && courses.Any())
                courseId = courses.First().Id;

            if (!courseId.HasValue)
                return View(new AnalyticsViewModel());

            var vm = await BuildAnalyticsAsync(
                courseId.Value, userId);
            return View(vm);
        }

        private async Task<AnalyticsViewModel>
            BuildAnalyticsAsync(int courseId, string userId)
        {
            var sessions = await _context.Sessions
                .Include(s => s.AttendanceRecords)
                    .ThenInclude(a => a.User)
                .Include(s => s.Course)
                .Where(s => s.CourseId == courseId
                    && (s.CreatedByUserId == userId
                        || User.IsInRole("Admin")))
                .OrderBy(s => s.SessionDate)
                .ToListAsync();

            var allRecords = sessions
                .SelectMany(s => s.AttendanceRecords)
                .ToList();

            // Enrolled students
            var enrolled = await _context.Enrollments
                .Where(e => e.CourseId == courseId)
                .Include(e => e.User)
                .ToListAsync();

            int totalStudents = enrolled.Count > 0
                ? enrolled.Count
                : Math.Max(1,
                    allRecords.Select(r => r.UserId)
                        .Distinct().Count());

            // Session Trends
            var trends = sessions.Select((s, idx) =>
                new SessionTrend
                {
                    Label = $"#{idx + 1} " +
                        s.SessionDate.ToString("dd/MM"),
                    Date = s.SessionDate,
                    AttendanceCount =
                        s.AttendanceRecords.Count,
                    AttendanceRate = totalStudents > 0
                        ? Math.Round(
                            (double)s.AttendanceRecords
                                .Count / totalStudents
                                * 100, 1)
                        : 0
                }).ToList();

            // Top Absent Students
            var topAbsent = enrolled
                .Select(e => {
                    int att = allRecords.Count(
                        r => r.UserId == e.UserId);
                    return new TopAbsentStudent
                    {
                        Name = e.User.FullName,
                        StudentId = e.User.StudentId,
                        TotalSessions = sessions.Count,
                        Attended = att,
                        AbsenceRate = sessions.Count > 0
                            ? Math.Round(
                                (double)(sessions.Count - att)
                                / sessions.Count * 100, 1)
                            : 0
                    };
                })
                .OrderByDescending(s => s.AbsenceRate)
                .Take(10)
                .ToList();

            // Weekly Heatmap
            var dayNames = new[]{
                "السبت","الأحد","الاثنين","الثلاثاء",
                "الأربعاء","الخميس","الجمعة" };

            var weeklyHeatmap = Enumerable.Range(0, 7)
                .Select(d => new DayHeatmap
                {
                    DayName = dayNames[d],
                    DayOfWeek = d,
                    HourlyCounts = Enumerable.Range(0, 24)
                        .Select(h => allRecords.Count(r =>
                            (int)r.ScannedAt
                                .DayOfWeek == d
                            && r.ScannedAt.Hour == h))
                        .ToList(),
                    TotalCount = allRecords.Count(
                        r => (int)r.ScannedAt
                            .DayOfWeek == d)
                }).ToList();

            // Hourly Distribution
            var hourly = Enumerable.Range(6, 16)
                .Select(h => new HourlyData
                {
                    Hour = h,
                    Count = allRecords.Count(
                        r => r.ScannedAt.Hour == h)
                }).ToList();

            var course = await _context.Courses
                .FindAsync(courseId);

            return new AnalyticsViewModel
            {
                CourseId = courseId,
                CourseName = course?.Name ?? "",
                TotalSessions = sessions.Count,
                TotalAttendance = allRecords.Count,
                OverallAttendanceRate = trends.Any()
    ? Math.Round(trends.Average(t => t.AttendanceRate), 1)
    : 0,
                SessionTrends = trends,
                TopAbsent = topAbsent,
                WeeklyHeatmap = weeklyHeatmap,
                HourlyDistribution = hourly,
            };
        }
    }
}