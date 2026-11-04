using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiteAttendance.Models;
using SiteAttendance.Services;

namespace SiteAttendance.Pages.Employees;

public class IndexModel : PageModel
{
    private readonly IEmployeeService _employeeService;

    public IndexModel(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    public List<Employee> Employees { get; set; } = new();

    public async Task OnGetAsync()
    {
        Employees = await _employeeService.GetAllAsync(activeOnly: false);
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var username = User.Identity?.Name ?? "ANONYMOUS";
        var result = await _employeeService.ToggleActiveAsync(id, username);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToPage();
    }
}

