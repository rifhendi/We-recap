using Microsoft.EntityFrameworkCore;
using SiteAttendance.Data;
using SiteAttendance.Models;

namespace SiteAttendance.Services;

public interface IAuditLogService
{
    Task LogAsync(string? userId, string action, string entityName, string? entityId, string description);
    Task<List<AuditLog>> GetRecentLogsAsync(int count = 50);
}

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(string? userId, string action, string entityName, string? entityId, string description)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId ?? "ANONYMOUS",
                Action = action.ToUpperInvariant(),
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal menulis audit log: {Action} on {Entity}", action, entityName);
        }
    }

    public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 50)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}

