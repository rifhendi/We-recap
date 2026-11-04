using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiteAttendance.Models;
using SiteAttendance.Services;

namespace SiteAttendance.Pages.Employees;

public class EditModel : PageModel
{
    private readonly IEmployeeService _employeeService;

    public EditModel(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kode Engineer wajib diisi.")]
        [Display(Name = "Kode Engineer")]
        [MaxLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama Lengkap wajib diisi.")]
        [Display(Name = "Nama Lengkap")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jabatan wajib diisi.")]
        [Display(Name = "Jabatan / Posisi")]
        [MaxLength(100)]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lokasi Site wajib diisi.")]
        [Display(Name = "Lokasi Site")]
        [MaxLength(100)]
        public string Site { get; set; } = string.Empty;

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var emp = await _employeeService.GetByIdAsync(id);
        if (emp == null)
        {
            TempData["ErrorMessage"] = "Data engineer tidak ditemukan.";
            return RedirectToPage("./Index");
        }

        Input = new InputModel
        {
            Id = emp.Id,
            EmployeeCode = emp.EmployeeCode,
            FullName = emp.FullName,
            Position = emp.Position,
            Site = emp.Site,
            IsActive = emp.IsActive
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var employee = new Employee
        {
            Id = Input.Id,
            EmployeeCode = Input.EmployeeCode.Trim().ToUpperInvariant(),
            FullName = Input.FullName.Trim(),
            Position = Input.Position.Trim(),
            Site = Input.Site.Trim(),
            IsActive = Input.IsActive
        };

        var username = User.Identity?.Name ?? "ANONYMOUS";
        var result = await _employeeService.UpdateAsync(employee, username);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToPage("./Index");
    }
}

