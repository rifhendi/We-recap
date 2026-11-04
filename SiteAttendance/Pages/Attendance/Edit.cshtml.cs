using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SiteAttendance.Models;
using SiteAttendance.Services;
using SiteAttendance.ViewModels;

namespace SiteAttendance.Pages.Attendance;

public class EditModel : PageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;

    public EditModel(IAttendanceService attendanceService, IEmployeeService employeeService)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
    }

    [BindProperty]
    public AttendanceInputModel Attendance { get; set; } = new();

    public SelectList EmployeeOptions { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var existing = await _attendanceService.GetByIdAsync(id);
        if (existing == null)
        {
            TempData["ErrorMessage"] = "Data absensi tidak ditemukan.";
            return RedirectToPage("./Index");
        }

        Attendance = new AttendanceInputModel
        {
            Id = existing.Id,
            EmployeeId = existing.EmployeeId,
            AttendanceDate = existing.AttendanceDate,
            CheckIn = existing.CheckIn,
            CheckOut = existing.CheckOut,
            Status = existing.Status,
            Description = existing.Description
        };

        await LoadSelectListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        var username = User.Identity?.Name ?? "ANONYMOUS";
        var result = await _attendanceService.UpdateAsync(Attendance, username);

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
        var employees = await _employeeService.GetAllAsync(activeOnly: false);
        EmployeeOptions = new SelectList(employees, nameof(Employee.Id), nameof(Employee.FullName), Attendance.EmployeeId);
    }
}

