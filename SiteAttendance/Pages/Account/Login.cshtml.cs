using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiteAttendance.Models;
using SiteAttendance.Services;

namespace SiteAttendance.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLog,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditLog = auditLog;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Username atau email wajib diisi.")]
        [Display(Name = "Username atau Email")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ingat Sesi Ini")]
        public bool RememberMe { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usernameOrEmail = Input.Username.Trim();
        var user = await _userManager.FindByNameAsync(usernameOrEmail) 
                   ?? await _userManager.FindByEmailAsync(usernameOrEmail);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Username atau password yang dimasukkan salah.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, Input.Password, Input.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            await _auditLog.LogAsync(user.UserName, "LOGIN", "Account", user.Id, "Login administrator berhasil.");
            _logger.LogInformation("Administrator '{Username}' berhasil login.", user.UserName);
            return LocalRedirect(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Password tidak cocok. Silakan coba kembali.");
        return Page();
    }
}

