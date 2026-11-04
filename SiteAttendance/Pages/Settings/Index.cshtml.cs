using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiteAttendance.Models;
using SiteAttendance.Services;

namespace SiteAttendance.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly IDatabaseBackupService _backupService;
    private readonly IAuditLogService _auditLogService;

    public IndexModel(IDatabaseBackupService backupService, IAuditLogService auditLogService)
    {
        _backupService = backupService;
        _auditLogService = auditLogService;
    }

    public List<AuditLog> AuditLogs { get; set; } = new();

    public async Task OnGetAsync()
    {
        AuditLogs = await _auditLogService.GetRecentLogsAsync(100);
    }

    public async Task<IActionResult> OnGetDownloadBackupAsync()
    {
        var backupBytes = await _backupService.GenerateBackupJsonAsync();
        var fileName = $"Backup_SiteAttendance_SiteRembang_{DateTime.UtcNow.AddHours(7):yyyyMMdd_HHmm}.json";

        var username = User.Identity?.Name ?? "ANONYMOUS";
        await _auditLogService.LogAsync(username, "BACKUP", "Database", null, $"Mengunduh file backup database: {fileName}");

        return File(backupBytes, "application/json", fileName);
    }

    public async Task<IActionResult> OnPostRestoreAsync(IFormFile? backupFile)
    {
        if (backupFile == null || backupFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Silakan pilih file backup (.json) yang valid.";
            return RedirectToPage();
        }

        using var reader = new StreamReader(backupFile.OpenReadStream(), Encoding.UTF8);
        var jsonContent = await reader.ReadToEndAsync();

        var username = User.Identity?.Name ?? "ANONYMOUS";
        var result = await _backupService.RestoreFromBackupJsonAsync(jsonContent, username);

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

