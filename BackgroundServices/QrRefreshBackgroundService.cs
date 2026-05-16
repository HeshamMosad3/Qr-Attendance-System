using QRAttendanceSystem.Services;

namespace QRAttendanceSystem.BackgroundServices
{
    public class QrRefreshBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QrRefreshBackgroundService> _logger;
        private readonly IConfiguration _config;

        // كل قد إيه يتجدد الـ QR (ثانية)
        private int RefreshSeconds => _config
            .GetValue<int>("AppSettings:DynamicQrRefreshSeconds", 30);

        public QrRefreshBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<QrRefreshBackgroundService> logger,
            IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "QR Refresh Background Service بدأ — كل {Sec} ثانية",
                RefreshSeconds);

            // انتظر شوية بعد بداية التطبيق
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dynamicQrService = scope.ServiceProvider
                        .GetRequiredService<IDynamicQrService>();

                    await dynamicQrService.RefreshAllActiveSessionsAsync();
                }
                catch (Exception ex) when (
                    !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex,
                        "خطأ في QR Refresh Background Service");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(RefreshSeconds),
                    stoppingToken);
            }

            _logger.LogInformation(
                "QR Refresh Background Service توقف.");
        }
    }
}