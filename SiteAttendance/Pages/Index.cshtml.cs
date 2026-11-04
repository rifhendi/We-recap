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
        // Default range: awal bulan berjalan s/d hari ini (atau akhir bulan berjalan)
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)); // UTC+7 WIB
        var start = StartDate ?? new DateOnly(today.Year, today.Month, 1);
        var end = EndDate ?? today;

        Dashboard = await _attendanceService.GetDashboardDataAsync(start, end, EmployeeId, Status, Search);

        var employees = await _employeeService.GetAllAsync(activeOnly: true);
        EmployeeOptions = new SelectList(employees, nameof(Employee.Id), nameof(Employee.FullName), EmployeeId);
    }
}
