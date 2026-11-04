using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiteAttendance.Models;
using SiteAttendance.Services;

namespace SiteAttendance.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditLogService _auditLog;

    public LogoutModel(SignInManager<ApplicationUser> signInManager, IAuditLogService auditLog)
    {
        _signInManager = signInManager;
        _auditLog = auditLog;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var username = User.Identity?.Name ?? "ANONYMOUS";
        await _signInManager.SignOutAsync();
        await _auditLog.LogAsync(username, "LOGOUT", "Account", null, "Administrator logout dari sesi.");
        return RedirectToPage("/Account/Login");
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await OnPostAsync();
    }
}

