using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace QRAttendanceSystem.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);

                // معالجة 404 و 403
                if (!context.Response.HasStarted)
                {
                    if (context.Response.StatusCode == 404)
                        await HandleStatusCodeAsync(context, 404);
                    else if (context.Response.StatusCode == 403)
                        await HandleStatusCodeAsync(context, 403);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في: {Path}", context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // لو الطلب JSON → رجع JSON
            if (context.Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    error = "حدث خطأ في الخادم",
                    detail = _env.IsDevelopment() ? ex.Message : null
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }

            // لو MVC → redirect للـ Error page
            context.Response.Redirect("/Home/Error?code=500");
        }

        private async Task HandleStatusCodeAsync(HttpContext context, int code)
        {
            // لو مش API request
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.Redirect($"/Home/Error?code={code}");
            }
            await Task.CompletedTask;
        }
    }

    // ===== التصحيح هنا: الـ Extension Method كانت مكتوبة بشكل خاطئ =====
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            // الصيغة الصحيحة هي UseMiddleware<Type>()
            return app.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}