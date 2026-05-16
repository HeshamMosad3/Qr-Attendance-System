using Microsoft.AspNetCore.Identity;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            // إنشاء الأدوار
            string[] roles = { "Admin", "Doctor", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // إنشاء Admin افتراضي
            const string adminEmail = "admin@qr.edu";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "مدير النظام",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // إنشاء مواد افتراضية
            if (!context.Courses.Any())
            {
                context.Courses.AddRange(
                    new Course { Name = "إدارة الأعمال", Code = "BUS101", AcademicYear = 1, CreditHours = 3 },
                    new Course { Name = "مبادئ الاقتصاد", Code = "ECO101", AcademicYear = 1, CreditHours = 3 },
                    new Course { Name = "إدارة التسويق", Code = "MKT201", AcademicYear = 2, CreditHours = 3 },
                    new Course { Name = "إدارة الموارد البشرية", Code = "HR201", AcademicYear = 2, CreditHours = 3 },
                    new Course { Name = "إدارة المشاريع", Code = "PM301", AcademicYear = 3, CreditHours = 3 },
                    new Course { Name = "ريادة الأعمال", Code = "ENT401", AcademicYear = 4, CreditHours = 3 }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}