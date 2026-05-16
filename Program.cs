using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Middleware;
using QRAttendanceSystem.Models;
using QRAttendanceSystem.Services;
using Serilog;
using QRAttendanceSystem.BackgroundServices;
using QRAttendanceSystem.Hubs;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IGpsVerificationService, GpsVerificationService>(); // ← ده اللي ناقص أو غلط
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IDynamicQrService, DynamicQrService>();
builder.Services.AddScoped<IPdfService, PdfService>();
// SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<IGpsVerificationService, GpsVerificationService>();
// Background Service
builder.Services.AddHostedService<QrRefreshBackgroundService>();

// Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie Config
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("DoctorOnly", p => p.RequireRole("Doctor"));
    options.AddPolicy("StudentOnly", p => p.RequireRole("Student"));
    options.AddPolicy("DoctorOrAdmin", p => p.RequireRole("Doctor", "Admin"));
});

// IHttpContextAccessor (for IP)
builder.Services.AddHttpContextAccessor();

// ===== Rate Limiting =====
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    // سياسة Login — 10 محاولات كل دقيقة
    options.AddFixedWindowLimiter("login", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // سياسة Attendance — 5 محاولات كل 30 ثانية
    options.AddFixedWindowLimiter("attendance", o =>
    {
        o.Window = TimeSpan.FromSeconds(30);
        o.PermitLimit = 5;
        o.QueueLimit = 0;
    });

    // سياسة عامة — 100 طلب في الدقيقة
    options.AddFixedWindowLimiter("general", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 100;
        o.QueueLimit = 5;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Callback لما يتجاوز الحد
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;

        // التحقق مما إذا كان الطلب عبارة عن API / JSON
        if (context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json"))
        {
            await context.HttpContext.Response.WriteAsync("{\"error\":\"Too many requests\"}", token);
        }
        else
        {
            context.HttpContext.Response.Redirect("/Home/Error?code=429");
        }
    };
});

var app = builder.Build();

// ===== Middleware Pipeline =====
// استخدام الـ Middleware بشكل صحيح لمعالجة الأخطاء
app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// تفعيل الـ Rate Limiting بعد الـ Routing
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();

// ===== Endpoints (Routing) =====

// SignalR Hub Route 
app.MapHub<AttendanceHub>("/hubs/attendance");

// Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .RequireRateLimiting("general");

// ===== Seed Database =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "خطأ أثناء تهيئة قاعدة البيانات");
    }
}

app.Run();