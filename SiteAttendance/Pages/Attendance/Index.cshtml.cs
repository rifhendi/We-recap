using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SiteAttendance.Models;
using SiteAttendance.Services;
using SiteAttendance.ViewModels;

namespace SiteAttendance.Pages.Attendance;

public class IndexModel : PageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;

    public IndexModel(IAttendanceService attendanceService, IEmployeeService employeeService)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
    }

    public List<Models.Attendance> Attendances { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public AttendanceFilterViewModel Filter { get; set; } = new();

    public SelectList EmployeeOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        // Default 1 bulan berjalan jika belum ditentukan
        Filter.StartDate ??= new DateOnly(today.Year, today.Month, 1);
        Filter.EndDate ??= today;

        Attendances = await _attendanceService.GetAttendancesAsync(Filter);

        var employees = await _employeeService.GetAllAsync(activeOnly: false);
        EmployeeOptions = new SelectList(employees, nameof(Employee.Id), nameof(Employee.FullName), Filter.EmployeeId);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var username = User.Identity?.Name ?? "ANONYMOUS";
        var result = await _attendanceService.DeleteAsync(id, username);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToPage(new
        {
            startDate = Filter.StartDate?.ToString("yyyy-MM-dd"),
            endDate = Filter.EndDate?.ToString("yyyy-MM-dd"),
            employeeId = Filter.EmployeeId,
            status = Filter.Status,
            search = Filter.Search
        });
    }
}

