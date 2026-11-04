using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SiteAttendance.Models;

namespace SiteAttendance.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Employee Configurations
        builder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.EmployeeCode).IsUnique();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Position).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Site).IsRequired().HasMaxLength(100);
        });

        // Attendance Configurations
        builder.Entity<Attendance>(entity =>
        {
            // Unique constraint: Satu engineer hanya boleh memiliki 1 record absensi per tanggal
            entity.HasIndex(a => new { a.EmployeeId, a.AttendanceDate })
                  .IsUnique()
                  .HasDatabaseName("IX_Attendance_Employee_Date_Unique");

            entity.HasIndex(a => a.AttendanceDate);
            entity.HasIndex(a => a.Status);

            entity.HasOne(a => a.Employee)
                  .WithMany(e => e.Attendances)
                  .HasForeignKey(a => a.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(a => a.TotalWorkHours)
                  .HasPrecision(5, 2);
        });

        // AuditLog Configurations
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(l => l.CreatedAt);
            entity.HasIndex(l => l.Action);
        });
    }
}

