using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SiteAttendance.Models;
using SiteAttendance.Services;
using SiteAttendance.ViewModels;

namespace SiteAttendance.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;
    private readonly IExportService _exportService;
    private readonly IAuditLogService _auditLog;

    public IndexModel(
        IAttendanceService attendanceService,
        IEmployeeService employeeService,
        IExportService exportService,
        IAuditLogService auditLog)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
        _exportService = exportService;
        _auditLog = auditLog;
    }

    public AttendanceReportViewModel Report { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateOnly? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EmployeeId { get; set; }

    public SelectList EmployeeOptions { get; set; } = null!;

    private static (DateOnly Start, DateOnly End) GetDefaultPeriod(DateOnly today)
    {
        // Siklus Cut-Off Absensi Site: 14 bulan sebelumnya s/d 15 bulan berjalan
        var start = today.Day <= 15
            ? new DateOnly(today.Year, today.Month, 14).AddMonths(-1)
            : new DateOnly(today.Year, today.Month, 14);

        var end = today.Day <= 15
            ? new DateOnly(today.Year, today.Month, 15)
            : new DateOnly(today.Year, today.Month, 15).AddMonths(1);

        return (start, end);
    }

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var (defStart, defEnd) = GetDefaultPeriod(today);

        var start = StartDate ?? defStart;
        var end = EndDate ?? defEnd;

        StartDate = start;
        EndDate = end;

        Report = await _attendanceService.GetReportDataAsync(start, end, EmployeeId);

        var employees = await _employeeService.GetAllAsync(activeOnly: false);
        EmployeeOptions = new SelectList(employees, nameof(Employee.Id), nameof(Employee.FullName), EmployeeId);
    }

    public async Task<IActionResult> OnGetDownloadPdfAsync(DateOnly? startDate, DateOnly? endDate, int? employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var (defStart, defEnd) = GetDefaultPeriod(today);

        var start = startDate ?? defStart;
        var end = endDate ?? defEnd;

        var reportData = await _attendanceService.GetReportDataAsync(start, end, employeeId);
        var pdfBytes = _exportService.ExportToPdf(reportData);
        var fileName = _exportService.GetFileName(reportData, "pdf");

        var username = User.Identity?.Name ?? "ANONYMOUS";
        await _auditLog.LogAsync(username, "EXPORT_PDF", "Report", null, $"Download laporan PDF: {fileName}");

        return File(pdfBytes, "application/pdf", fileName);
    }

    public async Task<IActionResult> OnGetDownloadExcelAsync(DateOnly? startDate, DateOnly? endDate, int? employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var (defStart, defEnd) = GetDefaultPeriod(today);

        var start = startDate ?? defStart;
        var end = endDate ?? defEnd;

        var reportData = await _attendanceService.GetReportDataAsync(start, end, employeeId);
        var excelBytes = _exportService.ExportToExcel(reportData);
        var fileName = _exportService.GetFileName(reportData, "xlsx");

        var username = User.Identity?.Name ?? "ANONYMOUS";
        await _auditLog.LogAsync(username, "EXPORT_EXCEL", "Report", null, $"Download laporan Excel: {fileName}");

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public async Task<IActionResult> OnGetDownloadCsvAsync(DateOnly? startDate, DateOnly? endDate, int? employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var (defStart, defEnd) = GetDefaultPeriod(today);

        var start = startDate ?? defStart;
        var end = endDate ?? defEnd;

        var reportData = await _attendanceService.GetReportDataAsync(start, end, employeeId);
        var csvBytes = _exportService.ExportToCsv(reportData);
        var fileName = _exportService.GetFileName(reportData, "csv");

        var username = User.Identity?.Name ?? "ANONYMOUS";
        await _auditLog.LogAsync(username, "EXPORT_CSV", "Report", null, $"Download laporan CSV: {fileName}");

        return File(csvBytes, "text/csv; charset=utf-8", fileName);
    }
}

