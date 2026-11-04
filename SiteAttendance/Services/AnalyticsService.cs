using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SiteAttendance.Data;
using SiteAttendance.DTOs;
using SiteAttendance.Models;
using SiteAttendance.ViewModels;

namespace SiteAttendance.Services;

public interface IAnalyticsService
{
    Task<MonthlyAttendanceSummaryDto> GetMonthlyAttendanceSummaryAsync(int year, int month);
    Task<EmployeeAttendanceHistoryDto> GetEmployeeAttendanceHistoryAsync(int employeeId, DateOnly startDate, DateOnly endDate);
    Task<AttendanceStatisticsDto> GetAttendanceStatisticsAsync(DateOnly startDate, DateOnly endDate, int? employeeId = null);
    Task<decimal> GetTotalWorkingHoursAsync(DateOnly startDate, DateOnly endDate, int? employeeId = null);
    Task<AttendanceTrendDto> GetAttendanceTrendAsync(DateOnly startDate, DateOnly endDate);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;

    public AnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MonthlyAttendanceSummaryDto> GetMonthlyAttendanceSummaryAsync(int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var attendances = await _context.Attendances
            .Include(a => a.Employee)
            .Where(a => a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var employees = await _context.Employees.AsNoTracking().Where(e => e.IsActive).ToListAsync();

        var totalDays = DateTime.DaysInMonth(year, month);
        var workDays = 0;
        for (int i = 1; i <= totalDays; i++)
        {
            var d = new DateTime(year, month, i);
            if (d.DayOfWeek != DayOfWeek.Sunday) workDays++;
        }

        var dto = new MonthlyAttendanceSummaryDto
        {
            Year = year,
            Month = month,
            MonthName = new DateTime(year, month, 1).ToString("MMMM yyyy", new CultureInfo("id-ID")),
            TotalDaysInMonth = totalDays,
            TotalWorkDays = workDays,
            TotalHadir = attendances.Count(a => a.Status == AttendanceStatus.Hadir),
            TotalIzin = attendances.Count(a => a.Status == AttendanceStatus.Izin),
            TotalSakit = attendances.Count(a => a.Status == AttendanceStatus.Sakit),
            TotalCuti = attendances.Count(a => a.Status == AttendanceStatus.Cuti),
            TotalLibur = attendances.Count(a => a.Status == AttendanceStatus.Libur),
            TotalHours = attendances.Sum(a => a.TotalWorkHours)
        };

        var totalEligible = attendances.Count(a => a.Status != AttendanceStatus.Libur);
        dto.OverallPresencePercentage = totalEligible > 0
            ? Math.Round((double)dto.TotalHadir / totalEligible * 100, 1)
            : 0;

        foreach (var emp in employees)
        {
            var empAtts = attendances.Where(a => a.EmployeeId == emp.Id).ToList();
            var empHadir = empAtts.Count(a => a.Status == AttendanceStatus.Hadir);
            var empEligible = empAtts.Count(a => a.Status != AttendanceStatus.Libur);

            dto.EmployeeMetrics.Add(new EmployeeMonthlyMetricDto
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.EmployeeCode,
                FullName = emp.FullName,
                Hadir = empHadir,
                Izin = empAtts.Count(a => a.Status == AttendanceStatus.Izin),
                Sakit = empAtts.Count(a => a.Status == AttendanceStatus.Sakit),
                Cuti = empAtts.Count(a => a.Status == AttendanceStatus.Cuti),
                Libur = empAtts.Count(a => a.Status == AttendanceStatus.Libur),
                TotalWorkHours = empAtts.Sum(a => a.TotalWorkHours),
                PresencePercentage = empEligible > 0 ? Math.Round((double)empHadir / empEligible * 100, 1) : 0
            });
        }

        return dto;
    }

    public async Task<EmployeeAttendanceHistoryDto> GetEmployeeAttendanceHistoryAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        var attendances = await _context.Attendances
            .Where(a => a.EmployeeId == employeeId && a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
            .OrderByDescending(a => a.AttendanceDate)
            .AsNoTracking()
            .ToListAsync();

        var dto = new EmployeeAttendanceHistoryDto
        {
            EmployeeId = employeeId,
            FullName = employee?.FullName ?? $"Engineer #{employeeId}",
            StartDate = startDate,
            EndDate = endDate,
            TotalRecords = attendances.Count,
            Records = attendances.Select(a => new AttendanceRecordDto
            {
                Id = a.Id,
                Date = a.AttendanceDate,
                DayName = AttendanceReportRow.GetIndonesianDay(a.AttendanceDate.DayOfWeek),
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut,
                Status = a.Status,
                TotalWorkHours = a.TotalWorkHours,
                Description = a.Description
            }).ToList()
        };

        return dto;
    }

    public async Task<AttendanceStatisticsDto> GetAttendanceStatisticsAsync(DateOnly startDate, DateOnly endDate, int? employeeId = null)
    {
        var query = _context.Attendances
            .Where(a => a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
            .AsNoTracking();

        if (employeeId.HasValue && employeeId.Value > 0)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        var list = await query.ToListAsync();

        var hadirList = list.Where(a => a.Status == AttendanceStatus.Hadir).ToList();
        var onTimeCount = hadirList.Count(a => a.CheckIn.HasValue && a.CheckIn.Value <= new TimeOnly(8, 0));

        var dto = new AttendanceStatisticsDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRecords = list.Count,
            TotalHadir = hadirList.Count,
            TotalIzin = list.Count(a => a.Status == AttendanceStatus.Izin),
            TotalSakit = list.Count(a => a.Status == AttendanceStatus.Sakit),
            TotalCuti = list.Count(a => a.Status == AttendanceStatus.Cuti),
            TotalLibur = list.Count(a => a.Status == AttendanceStatus.Libur),
            TotalWorkingHours = list.Sum(a => a.TotalWorkHours),
            AverageHoursPerDay = hadirList.Count > 0 ? Math.Round(list.Sum(a => a.TotalWorkHours) / hadirList.Count, 2) : 0,
            PunctualityRate = hadirList.Count > 0 ? Math.Round((double)onTimeCount / hadirList.Count * 100, 1) : 0
        };

        return dto;
    }

    public async Task<decimal> GetTotalWorkingHoursAsync(DateOnly startDate, DateOnly endDate, int? employeeId = null)
    {
        var query = _context.Attendances
            .Where(a => a.AttendanceDate >= startDate && a.AttendanceDate <= endDate);

        if (employeeId.HasValue && employeeId.Value > 0)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        return await query.SumAsync(a => a.TotalWorkHours);
    }

    public async Task<AttendanceTrendDto> GetAttendanceTrendAsync(DateOnly startDate, DateOnly endDate)
    {
        var attendances = await _context.Attendances
            .Where(a => a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var dates = attendances.Select(a => a.AttendanceDate).Distinct().OrderBy(d => d).ToList();

        var dto = new AttendanceTrendDto
        {
            PeriodLabel = $"{startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}",
            Points = dates.Select(d =>
            {
                var dayItems = attendances.Where(a => a.AttendanceDate == d).ToList();
                return new TrendPointDto
                {
                    Date = d,
                    Label = d.ToString("dd/MM"),
                    HadirCount = dayItems.Count(a => a.Status == AttendanceStatus.Hadir),
                    AbsentCount = dayItems.Count(a => a.Status != AttendanceStatus.Hadir && a.Status != AttendanceStatus.Libur),
                    TotalHours = dayItems.Sum(a => a.TotalWorkHours)
                };
            }).ToList()
        };

        return dto;
    }
}

