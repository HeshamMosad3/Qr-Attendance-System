using QRCoder;

namespace QRAttendanceSystem.Services
{
    public class QrCodeService : IQrCodeService
    {
        public string GenerateQrToken()
        {
            return Guid.NewGuid().ToString("N") + DateTime.UtcNow.Ticks.ToString("X");
        }

        public string GenerateQrCodeBase64(string url)
        {
            using var gen = new QRCodeGenerator();
            var data = gen.CreateQrCode(url,
                QRCodeGenerator.ECCLevel.Q);
            using var code = new PngByteQRCode(data);
            // زود الـ pixel size من 10 لـ 20
            return Convert.ToBase64String(code.GetGraphic(20));
        }
    }
}