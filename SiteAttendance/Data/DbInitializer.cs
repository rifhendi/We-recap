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
            }

            // 4. Sinkronisasi Data Absensi Periode 14 Oktober s/d 5 November 2026
            await SeedOctoberToNovemberAttendanceAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Terjadi kesalahan saat inisialisasi database.");
            throw;
        }
    }

    public static async Task SeedOctoberToNovemberAttendanceAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            var employees = await context.Employees.Where(e => e.IsActive).OrderBy(e => e.EmployeeCode).ToListAsync();
            if (!employees.Any()) return;

            var startDate = new DateOnly(2026, 10, 14);
            var endDate = new DateOnly(2026, 11, 5); // Hari ini

            var existingAttendances = await context.Attendances
                .Where(a => a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
                .ToListAsync();

            // Menit masuk realistis bervariasi antara 07:19 - 07:30
            int[] vinoMinutes = { 22, 19, 25, 20, 28, 21, 24, 19, 26, 23, 30, 20, 25, 22, 20, 25, 21 };
            int[] setiawanMinutes = { 20, 24, 27, 19, 25, 22, 29, 21, 23, 26, 28, 22, 24, 20, 23, 26, 22 };

            int weekdayIdx = 0;
            var toAdd = new List<Attendance>();

            for (var curDate = startDate; curDate <= endDate; curDate = curDate.AddDays(1))
            {
                var dayOfWeek = curDate.DayOfWeek;
                var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;

                int mIdx = weekdayIdx;
                if (!isWeekend)
                {
                    weekdayIdx++;
                }

                foreach (var emp in employees)
                {
                    var existing = existingAttendances.FirstOrDefault(a => a.EmployeeId == emp.Id && a.AttendanceDate == curDate);

                    if (isWeekend)
                    {
                        if (existing != null)
                        {
                            existing.Status = AttendanceStatus.Libur;
                            existing.CheckIn = null;
                            existing.CheckOut = null;
                            existing.TotalWorkHours = 0;
                            existing.Description = "Hari Libur Reguler";
                        }
                        else
                        {
                            toAdd.Add(new Attendance
                            {
                                EmployeeId = emp.Id,
                                AttendanceDate = curDate,
                                Status = AttendanceStatus.Libur,
                                CheckIn = null,
                                CheckOut = null,
                                TotalWorkHours = 0,
                                Description = "Hari Libur Reguler",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    else
                    {
                        // Hari kerja (Senin - Jumat)
                        int minute = emp.EmployeeCode == "EOS-001"
                            ? vinoMinutes[mIdx % vinoMinutes.Length]
                            : setiawanMinutes[mIdx % setiawanMinutes.Length];

                        var checkIn = new TimeOnly(7, minute);
                        var checkOut = new TimeOnly(16, 30);
                        decimal workHours = Math.Round((decimal)(checkOut - checkIn).TotalHours, 2);

                        if (existing != null)
                        {
                            existing.Status = AttendanceStatus.Hadir;
                            existing.CheckIn = checkIn;
                            existing.CheckOut = checkOut;
                            existing.TotalWorkHours = workHours;
                            // Kosongkan keterangan sesuai instruksi agar dapat diisi/dicek manual oleh user
                            if (string.IsNullOrWhiteSpace(existing.Description) ||
                                existing.Description.Contains("Preventive") ||
                                existing.Description.Contains("Monitoring") ||
                                existing.Description.Contains("Troubleshooting") ||
                                existing.Description.Contains("Koordinasi") ||
                                existing.Description.Contains("Standby") ||
                                existing.Description.Contains("Pengecekan"))
                            {
                                existing.Description = "";
                            }
                        }
                        else
                        {
                            toAdd.Add(new Attendance
                            {
                                EmployeeId = emp.Id,
                                AttendanceDate = curDate,
                                Status = AttendanceStatus.Hadir,
                                CheckIn = checkIn,
                                CheckOut = checkOut,
                                TotalWorkHours = workHours,
                                Description = "",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            if (toAdd.Count > 0)
            {
                await context.Attendances.AddRangeAsync(toAdd);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Sinkronisasi absensi periode 14 Oktober - 5 November 2026 berhasil diperbarui ({Count} data baru ditambahkan).", toAdd.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gagal sinkronisasi data absensi periode 14 Oktober - 5 November.");
        }
    }
}

