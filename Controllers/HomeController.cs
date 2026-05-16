using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public HomeController(
            UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction(
                    "Login", "Account");

            var user = await _userManager
                .GetUserAsync(User);
            if (user == null)
                return RedirectToAction(
                    "Login", "Account");

            if (User.IsInRole("Admin"))
                return RedirectToAction(
                    "Dashboard", "Admin");
            if (User.IsInRole("Doctor"))
                return RedirectToAction(
                    "Index", "Session");
            if (User.IsInRole("Student"))
                return RedirectToAction(
                    "MyRecords", "Attendance");

            return View();
        }

        public IActionResult Error(int? code)
        {
            ViewBag.Code = code ?? 500;
            return View();
        }
    }
}