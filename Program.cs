using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

// ===== Services =====
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IGpsVerificationService, GpsVerificationService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IDynamicQrService, DynamicQrService>();
builder.Services.AddScoped<IPdfService, PdfService>();

builder.Services.AddHostedService<QrRefreshBackgroundService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// ===== Database =====
// ===== Database =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL"); // متغير Railway

if (!string.IsNullOrEmpty(databaseUrl))
{
    var databaseUri = new Uri(databaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':');

    connectionString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};Ssl Mode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
// ===== Identity =====
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

// ===== Cookie =====
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ===== Authorization =====
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("DoctorOnly", p => p.RequireRole("Doctor"));
    options.AddPolicy("StudentOnly", p => p.RequireRole("Student"));
    options.AddPolicy("DoctorOrAdmin", p => p.RequireRole("Doctor", "Admin"));
});

// ===== Rate Limiting =====
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddFixedWindowLimiter("login", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddFixedWindowLimiter("attendance", o =>
    {
        o.Window = TimeSpan.FromSeconds(30);
        o.PermitLimit = 5;
        o.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("general", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 100;
        o.QueueLimit = 5;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        if (context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json"))
            await context.HttpContext.Response.WriteAsync("{\"error\":\"Too many requests\"}", token);
        else
            context.HttpContext.Response.Redirect("/Home/Error?code=429");
    };
});

// ===== Build App =====
var app = builder.Build();

// ===== Middleware Pipeline (الترتيب مهم جداً) =====

// 1. Exception Handling — لازم يكون أول حاجة
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseExceptionHandler("/Home/Error");
app.UseHsts();

// 2. HTTPS & Static Files
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

// 3. Routing
app.UseRouting();

// 4. Rate Limiting بعد Routing
app.UseRateLimiter();

// 5. Auth
app.UseAuthentication();
app.UseAuthorization();

// 6. Logging
app.UseSerilogRequestLogging();

// ===== Endpoints =====
app.MapHub<AttendanceHub>("/hubs/attendance");

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
        // لا تعمل throw في Production عشان التطبيق ميوقفش
    }
}

app.Run();