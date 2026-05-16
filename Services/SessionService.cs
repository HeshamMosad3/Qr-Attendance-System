using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;
using QRAttendanceSystem.ViewModels.Session;

namespace QRAttendanceSystem.Services
{
    public class SessionService : ISessionService
    {
        private readonly AppDbContext _context;
        private readonly IQrCodeService _qrService;
        private readonly IConfiguration _config;

        public SessionService(AppDbContext context, IQrCodeService qrService, IConfiguration config)
        {
            _context = context;
            _qrService = qrService;
            _config = config;
        }

        public async Task<Session> CreateSessionAsync(CreateSessionViewModel model, string doctorId)
        {
            int expiryMinutes = _config.GetValue<int>("AppSettings:QrExpiryMinutes", 30);

            var session = new Session
            {
                Title = model.Title,
                Type = model.Type,
                CourseId = model.CourseId,
                CreatedByUserId = doctorId,
                SessionDate = model.SessionDate,
                LateLimitMinutes = model.LateLimitMinutes,
                QrToken = _qrService.GenerateQrToken(),
                QrExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<List<Session>> GetDoctorSessionsAsync(string doctorId)
        {
            return await _context.Sessions
                .Include(s => s.Course)
                .Include(s => s.AttendanceRecords)
                .Where(s => s.CreatedByUserId == doctorId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Session?> GetSessionByIdAsync(int id)
        {
            return await _context.Sessions
                .Include(s => s.Course)
                .Include(s => s.CreatedBy)
                .Include(s => s.AttendanceRecords)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Session?> GetSessionByTokenAsync(string token)
        {
            return await _context.Sessions
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.QrToken == token);
        }

        public async Task<bool> DeactivateSessionAsync(int id, string doctorId)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null || session.CreatedByUserId != doctorId) return false;
            session.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> RefreshQrAsync(int id, string doctorId)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null || session.CreatedByUserId != doctorId) return string.Empty;

            int expiryMinutes = _config.GetValue<int>("AppSettings:QrExpiryMinutes", 30);
            session.QrToken = _qrService.GenerateQrToken();
            session.QrExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
            await _context.SaveChangesAsync();
            return session.QrToken;
        }

        public async Task<List<AttendanceRecord>> GetSessionAttendanceAsync(int id)
        {
            return await _context.AttendanceRecords
                .Include(a => a.User)
                .Where(a => a.SessionId == id)
                .OrderBy(a => a.ScannedAt)
                .ToListAsync();
        }
    }
}