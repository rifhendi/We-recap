using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SiteAttendance.Data;
using SiteAttendance.Models;

namespace SiteAttendance.Services;

public interface IDatabaseBackupService
{
    Task<byte[]> GenerateBackupJsonAsync();
    Task<(bool Success, string Message, int RestoredCount)> RestoreFromBackupJsonAsync(string jsonContent, string userId);
}

public class DatabaseBackupPackage
{
    public string SystemName { get; set; } = "Site Attendance Management";
    public string Version { get; set; } = "1.0";
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
    public string ExportedBy { get; set; } = string.Empty;

    public List<EmployeeBackupDto> Employees { get; set; } = new();
    public List<AttendanceBackupDto> Attendances { get; set; } = new();
    public List<AuditLogBackupDto> AuditLogs { get; set; } = new();
}

public class EmployeeBackupDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AttendanceBackupDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public DateOnly AttendanceDate { get; set; }
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Description { get; set; }
    public decimal TotalWorkHours { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AuditLogBackupDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(ApplicationDbContext context, IAuditLogService auditLog, ILogger<DatabaseBackupService> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<byte[]> GenerateBackupJsonAsync()
    {
        var employees = await _context.Employees.AsNoTracking().ToListAsync();
        var attendances = await _context.Attendances.Include(a => a.Employee).AsNoTracking().ToListAsync();
        var logs = await _context.AuditLogs.AsNoTracking().ToListAsync();

        var package = new DatabaseBackupPackage
        {
            ExportedBy = "Vino Rifhendi Setiawan",
            ExportedAtUtc = DateTime.UtcNow,
            Employees = employees.Select(e => new EmployeeBackupDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                Position = e.Position,
                Site = e.Site,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt
            }).ToList(),
            Attendances = attendances.Select(a => new AttendanceBackupDto
            {
                Id = a.Id,
                EmployeeCode = a.Employee.EmployeeCode,
                AttendanceDate = a.AttendanceDate,
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut,
                Status = a.Status,
                Description = a.Description,
                TotalWorkHours = a.TotalWorkHours,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            }).ToList(),
            AuditLogs = logs.Select(l => new AuditLogBackupDto
            {
                Id = l.Id,
                UserId = l.UserId,
                Action = l.Action,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                Description = l.Description,
                CreatedAt = l.CreatedAt
            }).ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.SerializeToUtf8Bytes(package, options);
    }

    public async Task<(bool Success, string Message, int RestoredCount)> RestoreFromBackupJsonAsync(string jsonContent, string userId)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var package = JsonSerializer.Deserialize<DatabaseBackupPackage>(jsonContent, options);

            if (package == null || package.Employees == null || package.Attendances == null)
            {
                return (false, "Format file backup tidak valid.", 0);
            }

            int restoredCount = 0;

            // 1. Sinkronisasi Data Employee
            var employeeMap = new Dictionary<string, int>(); // Code -> Id
            foreach (var empDto in package.Employees)
            {
                var existing = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeCode.ToLower() == empDto.EmployeeCode.ToLower());
                if (existing == null)
                {
                    existing = new Employee
                    {
                        EmployeeCode = empDto.EmployeeCode,
                        FullName = empDto.FullName,
                        Position = empDto.Position,
                        Site = string.IsNullOrWhiteSpace(empDto.Site) ? "Site Rembang" : empDto.Site,
                        IsActive = empDto.IsActive,
                        CreatedAt = empDto.CreatedAt != default ? empDto.CreatedAt : DateTime.UtcNow
                    };
                    _context.Employees.Add(existing);
                    await _context.SaveChangesAsync();
                }
                employeeMap[empDto.EmployeeCode.ToLower()] = existing.Id;
            }

            // 2. Sinkronisasi Data Absensi (Upsert berdasarkan EmployeeId dan AttendanceDate)
            foreach (var attDto in package.Attendances)
            {
                if (!employeeMap.TryGetValue(attDto.EmployeeCode.ToLower(), out var empId))
                {
                    continue;
                }

                var existingAtt = await _context.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == empId && a.AttendanceDate == attDto.AttendanceDate);
                if (existingAtt == null)
                {
                    existingAtt = new Attendance
                    {
                        EmployeeId = empId,
                        AttendanceDate = attDto.AttendanceDate,
                        CheckIn = attDto.CheckIn,
                        CheckOut = attDto.CheckOut,
                        Status = attDto.Status,
                        Description = attDto.Description,
                        TotalWorkHours = attDto.TotalWorkHours,
                        CreatedAt = attDto.CreatedAt != default ? attDto.CreatedAt : DateTime.UtcNow
                    };
                    _context.Attendances.Add(existingAtt);
                }
                else
                {
                    existingAtt.CheckIn = attDto.CheckIn;
                    existingAtt.CheckOut = attDto.CheckOut;
                    existingAtt.Status = attDto.Status;
                    existingAtt.Description = attDto.Description;
                    existingAtt.TotalWorkHours = attDto.TotalWorkHours;
                    existingAtt.UpdatedAt = DateTime.UtcNow;
                }

                restoredCount++;
            }

            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(userId, "RESTORE", "Database", null,
                $"Restore database berhasil: {restoredCount} record absensi dipulihkan dari file backup.");

            return (true, $"Berhasil memulihkan {restoredCount} data absensi.", restoredCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal melakukan restore database.");
            return (false, $"Gagal memulihkan database: {ex.Message}", 0);
        }
    }
}

