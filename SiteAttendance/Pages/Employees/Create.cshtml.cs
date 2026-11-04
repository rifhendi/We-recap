using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiteAttendance.Models;
using SiteAttendance.Services;

namespace SiteAttendance.Pages.Employees;

public class CreateModel : PageModel
{
    private readonly IEmployeeService _employeeService;

    public CreateModel(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Kode Engineer wajib diisi.")]
        [Display(Name = "Kode Engineer (Contoh: EOS-003)")]
        [MaxLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama Lengkap wajib diisi.")]
        [Display(Name = "Nama Lengkap")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jabatan wajib diisi.")]
        [Display(Name = "Jabatan / Posisi")]
        [MaxLength(100)]
        public string Position { get; set; } = "Engineer On Site";

        [Required(ErrorMessage = "Lokasi Site wajib diisi.")]
        [Display(Name = "Lokasi Site")]
        [MaxLength(100)]
        public string Site { get; set; } = "Site Rembang";

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var employee = new Employee
        {
            EmployeeCode = Input.EmployeeCode.Trim().ToUpperInvariant(),
            FullName = Input.FullName.Trim(),
            Position = Input.Position.Trim(),
            Site = Input.Site.Trim(),
            IsActive = Input.IsActive
        };

        var username = User.Identity?.Name ?? "ANONYMOUS";
        var result = await _employeeService.CreateAsync(employee, username);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToPage("./Index");
    }
}

