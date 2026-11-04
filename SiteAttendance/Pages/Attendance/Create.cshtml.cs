using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SiteAttendance.Models;
using SiteAttendance.Services;
using SiteAttendance.ViewModels;

namespace SiteAttendance.Pages.Attendance;

public class CreateModel : PageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;

    public CreateModel(IAttendanceService attendanceService, IEmployeeService employeeService)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
    }

    [BindProperty]
    public AttendanceInputModel Attendance { get; set; } = new();

    public SelectList EmployeeOptions { get; set; } = null!;

    public async Task OnGetAsync(int? employeeId = null, string? date = null)
    {
        if (employeeId.HasValue)
        {
            Attendance.EmployeeId = employeeId.Value;
        }

        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsedDate))
        {
            Attendance.AttendanceDate = parsedDate;
        }

        await LoadSelectListsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        var username = User.Identity?.Name ?? "ANONYMOUS";
        var result = await _attendanceService.CreateAsync(Attendance, username);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await LoadSelectListsAsync();
            return Page();
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToPage("./Index", new { employeeId = Attendance.EmployeeId });
    }

    private async Task LoadSelectListsAsync()
    {
        var employees = await _employeeService.GetAllAsync(activeOnly: true);
        EmployeeOptions = new SelectList(employees, nameof(Employee.Id), nameof(Employee.FullName), Attendance.EmployeeId);
    }
}

