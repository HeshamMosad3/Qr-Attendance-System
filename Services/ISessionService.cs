using QRAttendanceSystem.Models;
using QRAttendanceSystem.ViewModels.Session;

namespace QRAttendanceSystem.Services
{
    public interface ISessionService
    {
        Task<Session> CreateSessionAsync(CreateSessionViewModel model, string doctorId);
        Task<List<Session>> GetDoctorSessionsAsync(string doctorId);
        Task<Session?> GetSessionByIdAsync(int id);
        Task<Session?> GetSessionByTokenAsync(string token);
        Task<bool> DeactivateSessionAsync(int id, string doctorId);
        Task<string> RefreshQrAsync(int id, string doctorId);
        Task<List<AttendanceRecord>> GetSessionAttendanceAsync(int id);
    }
}