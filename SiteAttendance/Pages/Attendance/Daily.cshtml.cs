using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SiteAttendance.Data;
using SiteAttendance.Models;
using SiteAttendance.Services;
using System.Security.Claims;

namespace SiteAttendance.Pages.Attendance;

[Authorize]
public class DailyModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public DailyModel(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [BindProperty(SupportsGet = true)]
    public DateOnly TargetDate { get; set; }

    [BindProperty]
    public List<DailyEntryInput> Entries { get; set; } = new();

    public string IndonesianDayName => TargetDate.DayOfWeek switch
    {
        DayOfWeek.Sunday => "Minggu",
        DayOfWeek.Monday => "Senin",
        DayOfWeek.Tuesday => "Selasa",
        DayOfWeek.Wednesday => "Rabu",
        DayOfWeek.Thursday => "Kamis",
        DayOfWeek.Friday => "Jumat",
        DayOfWeek.Saturday => "Sabtu",
        _ => TargetDate.DayOfWeek.ToString()
    };

    public async Task<IActionResult> OnGetAsync(DateOnly? date)
    {
        // Default ke hari ini (Zona Waktu WIB UTC+7)
        var todayWib = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        TargetDate = date ?? todayWib;

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync();

        var existingAttendances = await _context.Attendances
            .Where(a => a.AttendanceDate == TargetDate)
            .AsNoTracking()
            .ToListAsync();

        bool isWeekend = TargetDate.DayOfWeek == DayOfWeek.Saturday || TargetDate.DayOfWeek == DayOfWeek.Sunday;

        Entries = new List<DailyEntryInput>();
        foreach (var emp in employees)
        {
            var existing = existingAttendances.FirstOrDefault(a => a.EmployeeId == emp.Id);

            if (existing != null)
            {
                Entries.Add(new DailyEntryInput
                {
                    AttendanceId = existing.Id,
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    FullName = emp.FullName,
                    Position = emp.Position,
                    Status = existing.Status,
                    CheckIn = existing.CheckIn?.ToString("HH:mm") ?? (existing.Status == AttendanceStatus.Hadir ? "07:00" : ""),
                    CheckOut = existing.CheckOut?.ToString("HH:mm") ?? (existing.Status == AttendanceStatus.Hadir ? "17:00" : ""),
                    Description = existing.Description ?? "",
                    IsAlreadyRecorded = true
                });
            }
            else
            {
                Entries.Add(new DailyEntryInput
                {
                    AttendanceId = null,
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    FullName = emp.FullName,
                    Position = emp.Position,
                    Status = isWeekend ? AttendanceStatus.Libur : AttendanceStatus.Hadir,
                    CheckIn = isWeekend ? "" : "07:00",
                    CheckOut = isWeekend ? "" : "17:00",
                    Description = isWeekend ? "Hari Libur Reguler" : "",
                    IsAlreadyRecorded = false
                });
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Entries == null || !Entries.Any())
        {
            TempData["ErrorMessage"] = "Tidak ada data absensi untuk disimpan.";
            return RedirectToPage(new { date = TargetDate.ToString("yyyy-MM-dd") });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "ADMIN";

        int savedCount = 0;
        foreach (var entry in Entries)
        {
            TimeOnly? checkInTime = null;
            TimeOnly? checkOutTime = null;
            decimal workHours = 0;

            if (entry.Status == AttendanceStatus.Hadir)
            {
                if (TimeOnly.TryParse(entry.CheckIn, out var parsedIn))
                {
                    checkInTime = parsedIn;
                }
                else
                {
                    checkInTime = new TimeOnly(7, 0);
                }

                if (TimeOnly.TryParse(entry.CheckOut, out var parsedOut))
                {
                    checkOutTime = parsedOut;
                }
                else
                {
                    checkOutTime = new TimeOnly(17, 0);
                }

                if (checkOutTime.Value < checkInTime.Value)
                {
                    TempData["ErrorMessage"] = $"Jam Pulang untuk {entry.FullName} tidak boleh lebih awal dari Jam Masuk.";
                    return RedirectToPage(new { date = TargetDate.ToString("yyyy-MM-dd") });
                }

                var diff = checkOutTime.Value - checkInTime.Value;
                workHours = Math.Round((decimal)diff.TotalHours, 2);
            }

            if (entry.AttendanceId.HasValue && entry.AttendanceId.Value > 0)
            {
                // Update
                var existing = await _context.Attendances.FindAsync(entry.AttendanceId.Value);
                if (existing != null)
                {
                    existing.Status = entry.Status;
                    existing.CheckIn = checkInTime;
                    existing.CheckOut = checkOutTime;
                    existing.TotalWorkHours = workHours;
                    existing.Description = entry.Description?.Trim();
                    existing.UpdatedAt = DateTime.UtcNow;
                    savedCount++;
                }
            }
            else
            {
                // Double check if already created by another session
                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EmployeeId == entry.EmployeeId && a.AttendanceDate == TargetDate);

                if (existing != null)
                {
                    existing.Status = entry.Status;
                    existing.CheckIn = checkInTime;
                    existing.CheckOut = checkOutTime;
                    existing.TotalWorkHours = workHours;
                    existing.Description = entry.Description?.Trim();
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newAtt = new Models.Attendance
                    {
                        EmployeeId = entry.EmployeeId,
                        AttendanceDate = TargetDate,
                        Status = entry.Status,
                        CheckIn = checkInTime,
                        CheckOut = checkOutTime,
                        TotalWorkHours = workHours,
                        Description = entry.Description?.Trim(),
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.Attendances.AddAsync(newAtt);
                }
                savedCount++;
            }
        }

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(userId, "DAILY_INPUT", "Attendance", TargetDate.ToString("yyyy-MM-dd"),
            $"Input Cepat Absensi Harian untuk {savedCount} Engineer tanggal {TargetDate:dd/MM/yyyy}");

        TempData["SuccessMessage"] = $"Absensi untuk {savedCount} Engineer pada {IndonesianDayName}, {TargetDate:dd MMMM yyyy} berhasil disimpan!";
        return RedirectToPage(new { date = TargetDate.ToString("yyyy-MM-dd") });
    }

    public class DailyEntryInput
    {
        public int? AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Hadir;
        public string CheckIn { get; set; } = "07:00";
        public string CheckOut { get; set; } = "17:00";
        public string Description { get; set; } = string.Empty;

        public bool IsAlreadyRecorded { get; set; }
    }
}

