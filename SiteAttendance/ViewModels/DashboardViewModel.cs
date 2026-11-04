using SiteAttendance.Models;

namespace SiteAttendance.ViewModels;

public class DashboardViewModel
{
    public int TotalEngineers { get; set; }
    public int TotalHadir { get; set; }
    public int TotalIzin { get; set; }
    public int TotalSakit { get; set; }
    public int TotalCuti { get; set; }
    public int TotalLibur { get; set; }
    public decimal TotalWorkHours { get; set; }

    public DateOnly FilterStartDate { get; set; }
    public DateOnly FilterEndDate { get; set; }
    public int? FilterEmployeeId { get; set; }
    public AttendanceStatus? FilterStatus { get; set; }
    public string? SearchTerm { get; set; }

    public string ActivePeriodText => $"{FilterStartDate:dd MMM yyyy} - {FilterEndDate:dd MMM yyyy}";

    public List<EmployeeSummaryItem> EmployeeSummaries { get; set; } = new();
    public List<Attendance> RecentAttendances { get; set; } = new();
    public List<AuditLog> RecentAuditLogs { get; set; } = new();

    public List<DailyTrendItem> DailyTrends { get; set; } = new();
}

public class EmployeeSummaryItem
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int HadirCount { get; set; }
    public int IzinCount { get; set; }
    public int SakitCount { get; set; }
    public int CutiCount { get; set; }
    public int LiburCount { get; set; }
    public decimal TotalHours { get; set; }
    public double PresenceRate { get; set; }
}

public class DailyTrendItem
{
    public DateOnly Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int HadirCount { get; set; }
    public int NonHadirCount { get; set; }
}

