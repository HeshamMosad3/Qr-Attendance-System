using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unique: طالب مرة واحدة في نفس الجلسة
            builder.Entity<AttendanceRecord>()
                .HasIndex(a => new { a.UserId, a.SessionId })
                .IsUnique();

            // Unique: طالب مسجل مرة واحدة في نفس المادة
            builder.Entity<Enrollment>()
                .HasIndex(e => new { e.UserId, e.CourseId })
                .IsUnique();

            // Session -> Course
            builder.Entity<Session>()
                .HasOne(s => s.Course)
                .WithMany(c => c.Sessions)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Session -> CreatedBy
            builder.Entity<Session>()
                .HasOne(s => s.CreatedBy)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // AttendanceRecord -> User
            builder.Entity<AttendanceRecord>()
                .HasOne(a => a.User)
                .WithMany(u => u.AttendanceRecords)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // AttendanceRecord -> Session
            builder.Entity<AttendanceRecord>()
                .HasOne(a => a.Session)
                .WithMany(s => s.AttendanceRecords)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Enrollment -> User
            builder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Enrollment -> Course
            builder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // AuditLog -> User (nullable)
            builder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}