using Microsoft.EntityFrameworkCore;
using SiteAttendance.Data;
using SiteAttendance.Models;
using SiteAttendance.ViewModels;

namespace SiteAttendance.Services;

public interface IAttendanceService
{
    Task<List<Attendance>> GetAttendancesAsync(AttendanceFilterViewModel filter);
    Task<Attendance?> GetByIdAsync(int id);
    Task<(bool Success, string Message, Attendance? Attendance)> CreateAsync(AttendanceInputModel model, string userId);
    Task<(bool Success, string Message)> UpdateAsync(AttendanceInputModel model, string userId);
    Task<(bool Success, string Message)> DeleteAsync(int id, string userId);
    Task<bool> ExistsAsync(int employeeId, DateOnly date, int? excludeId = null);
    Task<DashboardViewModel> GetDashboardDataAsync(DateOnly startDate, DateOnly endDate, int? employeeId = null, AttendanceStatus? status = null, string? search = null);
    Task<AttendanceReportViewModel> GetReportDataAsync(DateOnly startDate, DateOnly endDate, int? employeeId = null);
}

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public AttendanceService(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<List<Attendance>> GetAttendancesAsync(AttendanceFilterViewModel filter)
    {
        var query = _context.Attendances
            .Include(a => a.Employee)
            .AsNoTracking();

        if (filter.StartDate.HasValue)
        {
            query = query.Where(a => a.AttendanceDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(a => a.AttendanceDate <= filter.EndDate.Value);
        }

        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
        {
            query = query.Where(a => a.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(a =>
                a.Employee.FullName.ToLower().Contains(search) ||
                a.Employee.EmployeeCode.ToLower().Contains(search) ||
                (a.Description != null && a.Description.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(a => a.AttendanceDate)
            .ThenBy(a => a.Employee.FullName)
            .ToListAsync();
    }

    public async Task<Attendance?> GetByIdAsync(int id)
    {
        return await _context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> ExistsAsync(int employeeId, DateOnly date, int? excludeId = null)
    {
        var query = _context.Attendances.Where(a => a.EmployeeId == employeeId && a.AttendanceDate == date);
        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Message, Attendance? Attendance)> CreateAsync(AttendanceInputModel model, string userId)
    {
        // 1. Validasi engineer aktif
        var employee = await _context.Employees.FindAsync(model.EmployeeId);
        if (employee == null)
        {
            return (false, "Data Engineer tidak ditemukan.", null);
        }

        // 2. Cegah duplikasi tanggal dan engineer
        if (await ExistsAsync(model.EmployeeId, model.AttendanceDate))
        {
            return (false, $"Absensi untuk {employee.FullName} pada tanggal {model.AttendanceDate:dd/MM/yyyy} sudah pernah dicatat. Duplikasi tidak diizinkan.", null);
        }

        // 3. Validasi status Hadir
        decimal workHours = 0;
        if (model.Status == AttendanceStatus.Hadir)
        {
            if (!model.CheckIn.HasValue || !model.CheckOut.HasValue)
            {
                return (false, "Jam Masuk dan Jam Pulang wajib diisi untuk status Hadir.", null);
            }

            if (model.CheckOut.Value < model.CheckIn.Value)
            {
                return (false, "Jam Pulang tidak boleh lebih awal dari Jam Masuk.", null);
            }

            workHours = model.CalculateTotalWorkHours();
        }
        else
        {
            // Untuk izin, sakit, cuti, libur jam kerja tidak diwajibkan
            model.CheckIn = null;
            model.CheckOut = null;
            workHours = 0;
        }

        var attendance = new Attendance
        {
            EmployeeId = model.EmployeeId,
            AttendanceDate = model.AttendanceDate,
            CheckIn = model.CheckIn,
            CheckOut = model.CheckOut,
            Status = model.Status,
            Description = model.Description?.Trim(),
            TotalWorkHours = workHours,
            CreatedAt = DateTime.UtcNow
        };

        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(userId, "CREATE", "Attendance", attendance.Id.ToString(),
            $"Pencatatan absensi: {employee.FullName} - {attendance.AttendanceDate:dd/MM/yyyy} ({attendance.Status})");

        return (true, "Data absensi berhasil disimpan ke database PostgreSQL.", attendance);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(AttendanceInputModel model, string userId)
    {
        var existing = await _context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == model.Id);

        if (existing == null)
        {
            return (false, "Data absensi tidak ditemukan.");
        }

        // Cek duplikasi jika tanggal atau engineer diganti
        if (await ExistsAsync(model.EmployeeId, model.AttendanceDate, model.Id))
        {
            return (false, $"Absensi untuk engineer ini pada tanggal {model.AttendanceDate:dd/MM/yyyy} sudah ada pada data lain.");
        }

        decimal workHours = 0;
        if (model.Status == AttendanceStatus.Hadir)
        {
            if (!model.CheckIn.HasValue || !model.CheckOut.HasValue)
            {
                return (false, "Jam Masuk dan Jam Pulang wajib diisi untuk status Hadir.");
            }

            if (model.CheckOut.Value < model.CheckIn.Value)
            {
                return (false, "Jam Pulang tidak boleh lebih awal dari Jam Masuk.");
            }

            workHours = model.CalculateTotalWorkHours();
        }
        else
        {
            model.CheckIn = null;
            model.CheckOut = null;
            workHours = 0;
        }

        existing.EmployeeId = model.EmployeeId;
        existing.AttendanceDate = model.AttendanceDate;
        existing.CheckIn = model.CheckIn;
        existing.CheckOut = model.CheckOut;
        existing.Status = model.Status;
        existing.Description = model.Description?.Trim();
        existing.TotalWorkHours = workHours;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(userId, "UPDATE", "Attendance", existing.Id.ToString(),
            $"Pembaruan absensi: {existing.Employee.FullName} - {existing.AttendanceDate:dd/MM/yyyy} ({existing.Status})");

        return (true, "Data absensi berhasil diperbarui.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, string userId)
    {
        var attendance = await _context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attendance == null)
        {
            return (false, "Data absensi tidak ditemukan.");
        }

        var description = $"Menghapus absensi: {attendance.Employee.FullName} - {attendance.AttendanceDate:dd/MM/yyyy} ({attendance.Status})";

        _context.Attendances.Remove(attendance);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(userId, "DELETE", "Attendance", id.ToString(), description);

        return (true, "Data absensi berhasil dihapus.");
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(DateOnly startDate, DateOnly endDate, int? employeeId = null, AttendanceStatus? status = null, string? search = null)
    {
        var query = _context.Attendances
            .Include(a => a.Employee)
            .Where(a => a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
            .AsNoTracking();

        if (employeeId.HasValue && employeeId.Value > 0)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a =>
                a.Employee.FullName.ToLower().Contains(s) ||
                a.Employee.EmployeeCode.ToLower().Contains(s) ||
                (a.Description != null && a.Description.ToLower().Contains(s)));
        }

        var attendances = await query.ToListAsync();
        var allEmployees = await _context.Employees.AsNoTracking().Where(e => e.IsActive).OrderBy(e => e.EmployeeCode).ToListAsync();

        var vm = new DashboardViewModel
        {
            TotalEngineers = allEmployees.Count,
            TotalHadir = attendances.Count(a => a.Status == AttendanceStatus.Hadir),
            TotalIzin = attendances.Count(a => a.Status == AttendanceStatus.Izin),
            TotalSakit = attendances.Count(a => a.Status == AttendanceStatus.Sakit),
            TotalCuti = attendances.Count(a => a.Status == AttendanceStatus.Cuti),
            TotalLibur = attendances.Count(a => a.Status == AttendanceStatus.Libur),
            TotalWorkHours = attendances.Sum(a => a.TotalWorkHours),

            FilterStartDate = startDate,
            FilterEndDate = endDate,
            FilterEmployeeId = employeeId,
            FilterStatus = status,
            SearchTerm = search,

            RecentAttendances = attendances.OrderByDescending(a => a.AttendanceDate).ThenByDescending(a => a.CreatedAt).Take(10).ToList(),
            RecentAuditLogs = await _context.AuditLogs.AsNoTracking().OrderByDescending(l => l.CreatedAt).Take(8).ToListAsync()
        };

        // Ringkasan per Engineer
        foreach (var emp in allEmployees)
        {
            var empAttendances = attendances.Where(a => a.EmployeeId == emp.Id).ToList();
            var hadir = empAttendances.Count(a => a.Status == AttendanceStatus.Hadir);
            var totalNonLibur = empAttendances.Count(a => a.Status != AttendanceStatus.Libur);

            vm.EmployeeSummaries.Add(new EmployeeSummaryItem
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.EmployeeCode,
                FullName = emp.FullName,
                Position = emp.Position,
                HadirCount = hadir,
                IzinCount = empAttendances.Count(a => a.Status == AttendanceStatus.Izin),
                SakitCount = empAttendances.Count(a => a.Status == AttendanceStatus.Sakit),
                CutiCount = empAttendances.Count(a => a.Status == AttendanceStatus.Cuti),
                LiburCount = empAttendances.Count(a => a.Status == AttendanceStatus.Libur),
                TotalHours = empAttendances.Sum(a => a.TotalWorkHours),
                PresenceRate = totalNonLibur > 0 ? Math.Round((double)hadir / totalNonLibur * 100, 1) : 0
            });
        }

        // Tren Harian (Grouping by Date)
        var dates = attendances.Select(a => a.AttendanceDate).Distinct().OrderBy(d => d).ToList();
        foreach (var d in dates)
        {
            var dayAtts = attendances.Where(a => a.AttendanceDate == d).ToList();
            vm.DailyTrends.Add(new DailyTrendItem
            {
                Date = d,
                DayName = AttendanceReportRow.GetIndonesianDay(d.DayOfWeek),
                HadirCount = dayAtts.Count(a => a.Status == AttendanceStatus.Hadir),
                NonHadirCount = dayAtts.Count(a => a.Status != AttendanceStatus.Hadir && a.Status != AttendanceStatus.Libur)
            });
        }

        return vm;
    }

    public async Task<AttendanceReportViewModel> GetReportDataAsync(DateOnly startDate, DateOnly endDate, int? employeeId = null)
    {
        var vm = new AttendanceReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
            EmployeeId = employeeId
        };

        var empQuery = _context.Employees.AsNoTracking().Where(e => e.IsActive);
        if (employeeId.HasValue && employeeId.Value > 0)
        {
            empQuery = empQuery.Where(e => e.Id == employeeId.Value);
            vm.SelectedEmployee = await _context.Employees.FindAsync(employeeId.Value);
        }

        var employees = await empQuery.OrderBy(e => e.EmployeeCode).ToListAsync();

        var attendances = await _context.Attendances
            .Include(a => a.Employee)
            .Where(a => a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
            .AsNoTracking()
            .ToListAsync();

        vm.TotalHadir = attendances.Count(a => a.Status == AttendanceStatus.Hadir);
        vm.TotalIzin = attendances.Count(a => a.Status == AttendanceStatus.Izin);
        vm.TotalSakit = attendances.Count(a => a.Status == AttendanceStatus.Sakit);
        vm.TotalCuti = attendances.Count(a => a.Status == AttendanceStatus.Cuti);
        vm.TotalLibur = attendances.Count(a => a.Status == AttendanceStatus.Libur);
        vm.TotalWorkHours = attendances.Sum(a => a.TotalWorkHours);

        foreach (var emp in employees)
        {
            var empAtts = attendances
                .Where(a => a.EmployeeId == emp.Id)
                .OrderBy(a => a.AttendanceDate)
                .ToList();

            string titlePos = emp.Position.Contains("Rembang", StringComparison.OrdinalIgnoreCase)
                ? emp.Position
                : $"{emp.Position} Rembang";

            var section = new EmployeeReportSection
            {
                Employee = emp,
                HadirCount = empAtts.Count(a => a.Status == AttendanceStatus.Hadir),
                IzinCount = empAtts.Count(a => a.Status == AttendanceStatus.Izin),
                SakitCount = empAtts.Count(a => a.Status == AttendanceStatus.Sakit),
                CutiCount = empAtts.Count(a => a.Status == AttendanceStatus.Cuti),
                LiburCount = empAtts.Count(a => a.Status == AttendanceStatus.Libur),
                TotalHours = empAtts.Sum(a => a.TotalWorkHours),
                CreatedBy = new SignatureBlock
                {
                    Header = "Dibuat oleh,",
                    Company = "PT Kirana Sakti Komputindo",
                    Name = emp.FullName,
                    Title = titlePos
                }
            };

            int no = 1;
            for (var curDate = startDate; curDate <= endDate; curDate = curDate.AddDays(1))
            {
                var att = empAtts.FirstOrDefault(a => a.AttendanceDate == curDate);
                var dayOfWeek = curDate.DayOfWeek;
                var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;

                if (att != null)
                {
                    string desc = !string.IsNullOrWhiteSpace(att.Description)
                        ? att.Description
                        : (att.Status == AttendanceStatus.Hadir ? "" : (isWeekend ? "-" : att.Status.ToString()));

                    section.Rows.Add(new AttendanceReportRow
                    {
                        No = no++,
                        Date = curDate,
                        DayName = AttendanceReportRow.GetIndonesianDay(dayOfWeek),
                        CheckInText = att.CheckIn.HasValue ? att.CheckIn.Value.ToString("HH:mm") : "-",
                        CheckOutText = att.CheckOut.HasValue ? att.CheckOut.Value.ToString("HH:mm") : "-",
                        Status = att.Status,
                        WorkHours = att.TotalWorkHours,
                        Description = desc
                    });
                }
                else
                {
                    section.Rows.Add(new AttendanceReportRow
                    {
                        No = no++,
                        Date = curDate,
                        DayName = AttendanceReportRow.GetIndonesianDay(dayOfWeek),
                        CheckInText = "-",
                        CheckOutText = "-",
                        Status = isWeekend ? AttendanceStatus.Libur : AttendanceStatus.Hadir,
                        WorkHours = 0,
                        Description = isWeekend ? "-" : ""
                    });
                }
            }

            vm.Sections.Add(section);
        }

        return vm;
    }
}

