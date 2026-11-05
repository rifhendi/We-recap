using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SiteAttendance.Models;
using SiteAttendance.Services;
using SiteAttendance.ViewModels;

namespace SiteAttendance.Pages;

public class IndexModel : PageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;

    public IndexModel(IAttendanceService attendanceService, IEmployeeService employeeService)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
    }

    public DashboardViewModel Dashboard { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateOnly? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EmployeeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public AttendanceStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public SelectList EmployeeOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        // Default range: awal siklus cut-off absensi site (14 bulan sebelumnya jika tgl <= 15) s/d hari ini
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)); // UTC+7 WIB
        var defaultStart = today.Day <= 15
            ? new DateOnly(today.Year, today.Month, 14).AddMonths(-1)
            : new DateOnly(today.Year, today.Month, 14);

        var start = StartDate ?? defaultStart;
        var end = EndDate ?? today;

        Dashboard = await _attendanceService.GetDashboardDataAsync(start, end, EmployeeId, Status, Search);

        var employees = await _employeeService.GetAllAsync(activeOnly: true);
        EmployeeOptions = new SelectList(employees, nameof(Employee.Id), nameof(Employee.FullName), EmployeeId);
    }
}
