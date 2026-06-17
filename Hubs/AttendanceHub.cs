using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace QRAttendanceSystem.Hubs
{
    [Authorize]
    public class AttendanceHub : Hub
    {
        // الدكتور بيدخل group خاص بالجلسة عشان يستقبل تحديثات الـ QR

        public async Task JoinSession(string sessionId)
        {
            // لازم الجروب يكون اسمه session_ متبوعاً برقم الجلسة عشان يطابق الـ DynamicQrService
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
        }

        public async Task LeaveSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");
        }
    }
}