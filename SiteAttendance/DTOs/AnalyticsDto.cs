using SiteAttendance.Models;

namespace SiteAttendance.DTOs;

public class MonthlyAttendanceSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int TotalDaysInMonth { get; set; }
    public int TotalWorkDays { get; set; }
    public int TotalHadir { get; set; }
    public int TotalIzin { get; set; }
    public int TotalSakit { get; set; }
    public int TotalCuti { get; set; }
    public int TotalLibur { get; set; }
    public decimal TotalHours { get; set; }
    public double OverallPresencePercentage { get; set; }
    public List<EmployeeMonthlyMetricDto> EmployeeMetrics { get; set; } = new();
}

public class EmployeeMonthlyMetricDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Hadir { get; set; }
    public int Izin { get; set; }
    public int Sakit { get; set; }
    public int Cuti { get; set; }
    public int Libur { get; set; }
    public decimal TotalWorkHours { get; set; }
    public double PresencePercentage { get; set; }
}

public class EmployeeAttendanceHistoryDto
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalRecords { get; set; }
    public List<AttendanceRecordDto> Records { get; set; } = new();
}

public class AttendanceRecordDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public AttendanceStatus Status { get; set; }
    public decimal TotalWorkHours { get; set; }
    public string? Description { get; set; }
}

public class AttendanceStatisticsDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalRecords { get; set; }
    public int TotalHadir { get; set; }
    public int TotalIzin { get; set; }
    public int TotalSakit { get; set; }
    public int TotalCuti { get; set; }
    public int TotalLibur { get; set; }
    public decimal TotalWorkingHours { get; set; }
    public decimal AverageHoursPerDay { get; set; }
    public double PunctualityRate { get; set; } // check-in <= 08:00
}

public class AttendanceTrendDto
{
    public string PeriodLabel { get; set; } = string.Empty;
    public List<TrendPointDto> Points { get; set; } = new();
}

public class TrendPointDto
{
    public string Label { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int HadirCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal TotalHours { get; set; }
}

