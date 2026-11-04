using Microsoft.EntityFrameworkCore;
using SiteAttendance.Data;
using SiteAttendance.Models;

namespace SiteAttendance.Services;

public interface IEmployeeService
{
    Task<List<Employee>> GetAllAsync(bool activeOnly = false);
    Task<Employee?> GetByIdAsync(int id);
    Task<(bool Success, string Message, Employee? Employee)> CreateAsync(Employee employee, string userId);
    Task<(bool Success, string Message)> UpdateAsync(Employee employee, string userId);
    Task<(bool Success, string Message)> ToggleActiveAsync(int id, string userId);
}

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public EmployeeService(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<List<Employee>> GetAllAsync(bool activeOnly = false)
    {
        var query = _context.Employees.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(e => e.IsActive);
        }
        return await query.OrderBy(e => e.EmployeeCode).ToListAsync();
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        return await _context.Employees
            .Include(e => e.Attendances)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<(bool Success, string Message, Employee? Employee)> CreateAsync(Employee employee, string userId)
    {
        // Validasi kode unik
        if (await _context.Employees.AnyAsync(e => e.EmployeeCode.ToLower() == employee.EmployeeCode.ToLower()))
        {
            return (false, $"Kode Engineer '{employee.EmployeeCode}' sudah digunakan.", null);
        }

        employee.CreatedAt = DateTime.UtcNow;
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(userId, "CREATE", "Employee", employee.Id.ToString(),
            $"Menambahkan engineer baru: {employee.FullName} ({employee.EmployeeCode})");

        return (true, "Data Engineer berhasil ditambahkan.", employee);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Employee employee, string userId)
    {
        var existing = await _context.Employees.FindAsync(employee.Id);
        if (existing == null)
        {
            return (false, "Data Engineer tidak ditemukan.");
        }

        // Cek duplikasi kode pada ID lain
        if (await _context.Employees.AnyAsync(e => e.EmployeeCode.ToLower() == employee.EmployeeCode.ToLower() && e.Id != employee.Id))
        {
            return (false, $"Kode Engineer '{employee.EmployeeCode}' sudah digunakan oleh engineer lain.");
        }

        existing.EmployeeCode = employee.EmployeeCode;
        existing.FullName = employee.FullName;
        existing.Position = employee.Position;
        existing.Site = employee.Site;
        existing.IsActive = employee.IsActive;

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(userId, "UPDATE", "Employee", existing.Id.ToString(),
            $"Memperbarui data engineer: {existing.FullName} ({existing.EmployeeCode})");

        return (true, "Data Engineer berhasil diperbarui.");
    }

    public async Task<(bool Success, string Message)> ToggleActiveAsync(int id, string userId)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return (false, "Data Engineer tidak ditemukan.");
        }

        employee.IsActive = !employee.IsActive;
        await _context.SaveChangesAsync();

        var statusText = employee.IsActive ? "diaktifkan" : "dinonaktifkan";
        await _auditLog.LogAsync(userId, "TOGGLE_ACTIVE", "Employee", employee.Id.ToString(),
            $"Status engineer {employee.FullName} telah {statusText}.");

        return (true, $"Status Engineer berhasil {statusText}.");
    }
}

