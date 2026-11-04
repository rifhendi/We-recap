using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SiteAttendance.Models;

namespace SiteAttendance.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        try
        {
            // Terapkan migrasi otomatis jika mendukung, atau fallback ke EnsureCreated
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            // 1. Inisialisasi Role
            string adminRole = "Administrator";
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
                logger.LogInformation("Role '{Role}' berhasil dibuat.", adminRole);
            }

            // 2. Inisialisasi User Administrator (Vino Rifhendi Setiawan)
            string adminUsername = "vino.setiawan";
            string adminEmail = "vino.setiawan@siterembang.local";
            string defaultPassword = Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD") ?? "AdminSite2026!";

            var existingAdmin = await userManager.FindByNameAsync(adminUsername);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminUsername,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Vino Rifhendi Setiawan",
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(adminUser, defaultPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                    logger.LogInformation("Administrator '{Username}' berhasil diinisialisasi.", adminUsername);

                    context.AuditLogs.Add(new AuditLog
                    {
                        UserId = "SYSTEM",
                        Action = "SEED",
                        EntityName = "ApplicationUser",
                        EntityId = adminUser.Id,
                        Description = $"Inisialisasi akun Administrator: {adminUsername}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    logger.LogError("Gagal membuat user admin: {Errors}", errors);
                }
            }

            // 3. Inisialisasi Data Engineer On Site
            if (!await context.Employees.AnyAsync())
            {
                var employees = new List<Employee>
                {
                    new Employee
                    {
                        EmployeeCode = "EOS-001",
                        FullName = "Vino Rifhendi Setiawan",
                        Position = "Leader / Engineer On Site",
                        Site = "Site Rembang",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Employee
                    {
                        EmployeeCode = "EOS-002",
                        FullName = "Muhammad Setiawan",
                        Position = "Engineer On Site",
                        Site = "Site Rembang",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Employees.AddRangeAsync(employees);
                await context.SaveChangesAsync();
                logger.LogInformation("2 Data Engineer On Site berhasil diinisialisasi.");

                // 4. Inisialisasi Data Absensi Contoh (Periode berjalan 1 - 4 November 2026)
                var vino = employees[0];
                var setiawan = employees[1];

                var sampleAttendances = new List<Attendance>
                {
                    // Hari 1: 01 Nov 2026 (Minggu - Libur)
                    new Attendance
                    {
                        EmployeeId = vino.Id,
                        AttendanceDate = new DateOnly(2026, 11, 1),
                        Status = AttendanceStatus.Libur,
                        CheckIn = null,
                        CheckOut = null,
                        TotalWorkHours = 0,
                        Description = "Hari Libur Reguler",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Attendance
                    {
                        EmployeeId = setiawan.Id,
                        AttendanceDate = new DateOnly(2026, 11, 1),
                        Status = AttendanceStatus.Libur,
                        CheckIn = null,
                        CheckOut = null,
                        TotalWorkHours = 0,
                        Description = "Hari Libur Reguler",
                        CreatedAt = DateTime.UtcNow
                    },

                    // Hari 2: 02 Nov 2026 (Senin - Hadir normal)
                    new Attendance
                    {
                        EmployeeId = vino.Id,
                        AttendanceDate = new DateOnly(2026, 11, 2),
                        Status = AttendanceStatus.Hadir,
                        CheckIn = new TimeOnly(8, 0),
                        CheckOut = new TimeOnly(17, 0),
                        TotalWorkHours = 9.0m,
                        Description = "Monitoring jaringan Access Point CCR Rembang",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Attendance
                    {
                        EmployeeId = setiawan.Id,
                        AttendanceDate = new DateOnly(2026, 11, 2),
                        Status = AttendanceStatus.Hadir,
                        CheckIn = new TimeOnly(8, 5),
                        CheckOut = new TimeOnly(17, 10),
                        TotalWorkHours = 9.08m,
                        Description = "Preventive maintenance Switch Rack lantai 2",
                        CreatedAt = DateTime.UtcNow
                    },

                    // Hari 3: 03 Nov 2026 (Selasa - Hadir normal)
                    new Attendance
                    {
                        EmployeeId = vino.Id,
                        AttendanceDate = new DateOnly(2026, 11, 3),
                        Status = AttendanceStatus.Hadir,
                        CheckIn = new TimeOnly(7, 55),
                        CheckOut = new TimeOnly(17, 0),
                        TotalWorkHours = 9.08m,
                        Description = "Troubleshooting koneksi Wi-Fi area Workshop",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Attendance
                    {
                        EmployeeId = setiawan.Id,
                        AttendanceDate = new DateOnly(2026, 11, 3),
                        Status = AttendanceStatus.Hadir,
                        CheckIn = new TimeOnly(8, 0),
                        CheckOut = new TimeOnly(17, 0),
                        TotalWorkHours = 9.0m,
                        Description = "Pengecekan kabel fiber optic converter Site Rembang",
                        CreatedAt = DateTime.UtcNow
                    },

                    // Hari 4: 04 Nov 2026 (Rabu - Hari ini)
                    new Attendance
                    {
                        EmployeeId = vino.Id,
                        AttendanceDate = new DateOnly(2026, 11, 4),
                        Status = AttendanceStatus.Hadir,
                        CheckIn = new TimeOnly(7, 50),
                        CheckOut = new TimeOnly(17, 0),
                        TotalWorkHours = 9.17m,
                        Description = "Koordinasi dengan Project Manager MS WiFi dan site check",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Attendance
                    {
                        EmployeeId = setiawan.Id,
                        AttendanceDate = new DateOnly(2026, 11, 4),
                        Status = AttendanceStatus.Hadir,
                        CheckIn = new TimeOnly(8, 0),
                        CheckOut = new TimeOnly(17, 5),
                        TotalWorkHours = 9.08m,
                        Description = "Standby operasional & ticket handling Manage Service SIG",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Attendances.AddRangeAsync(sampleAttendances);
                await context.SaveChangesAsync();
                logger.LogInformation("Sampel absensi awal berhasil diinput.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Terjadi kesalahan saat inisialisasi database.");
            throw;
        }
    }
}

