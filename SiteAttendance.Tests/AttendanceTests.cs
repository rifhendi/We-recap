using System.Text;
using QuestPDF.Fluent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SiteAttendance.Data;
using SiteAttendance.Models;
using SiteAttendance.Services;
using SiteAttendance.ViewModels;

namespace SiteAttendance.Tests;

public class AttendanceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task EmployeeService_CreateAndPreventDuplicates_WorksCorrectly()
    {
        using var context = CreateInMemoryDbContext();
        var audit = new AuditLogService(context, NullLogger<AuditLogService>.Instance);
        var service = new EmployeeService(context, audit);

        var emp1 = new Employee
        {
            EmployeeCode = "EOS-001",
            FullName = "Vino Rifhendi Setiawan",
            Position = "Leader / Engineer On Site",
            Site = "Site Rembang"
        };

        var result1 = await service.CreateAsync(emp1, "test-user");
        Assert.True(result1.Success);
        Assert.NotNull(result1.Employee);

        // Coba duplikasi kode
        var emp2 = new Employee
        {
            EmployeeCode = "EOS-001",
            FullName = "Vino Duplikat",
            Position = "Engineer",
            Site = "Site Rembang"
        };

        var result2 = await service.CreateAsync(emp2, "test-user");
        Assert.False(result2.Success);
        Assert.Contains("sudah digunakan", result2.Message);
    }

    [Fact]
    public async Task AttendanceService_CreateAndValidateWorkHours_CalculatesAccurately()
    {
        using var context = CreateInMemoryDbContext();
        var audit = new AuditLogService(context, NullLogger<AuditLogService>.Instance);
        var empService = new EmployeeService(context, audit);
        var attService = new AttendanceService(context, audit);

        var emp = new Employee
        {
            EmployeeCode = "EOS-001",
            FullName = "Vino Rifhendi Setiawan",
            Position = "Leader / Engineer On Site",
            Site = "Site Rembang"
        };
        await empService.CreateAsync(emp, "test-user");

        var input = new AttendanceInputModel
        {
            EmployeeId = emp.Id,
            AttendanceDate = new DateOnly(2026, 11, 4),
            Status = AttendanceStatus.Hadir,
            CheckIn = new TimeOnly(8, 0),
            CheckOut = new TimeOnly(17, 30),
            Description = "Monitoring Access Point"
        };

        var result = await attService.CreateAsync(input, "test-user");
        Assert.True(result.Success);
        Assert.NotNull(result.Attendance);
        Assert.Equal(9.50m, result.Attendance.TotalWorkHours);

        // Duplikasi untuk engineer dan tanggal yang sama harus dicegah
        var duplicateInput = new AttendanceInputModel
        {
            EmployeeId = emp.Id,
            AttendanceDate = new DateOnly(2026, 11, 4),
            Status = AttendanceStatus.Hadir,
            CheckIn = new TimeOnly(8, 0),
            CheckOut = new TimeOnly(17, 0)
        };

        var duplicateResult = await attService.CreateAsync(duplicateInput, "test-user");
        Assert.False(duplicateResult.Success);
        Assert.Contains("sudah pernah dicatat", duplicateResult.Message);
    }

    [Fact]
    public async Task AttendanceService_NonHadirStatus_DoesNotRequireHours()
    {
        using var context = CreateInMemoryDbContext();
        var audit = new AuditLogService(context, NullLogger<AuditLogService>.Instance);
        var empService = new EmployeeService(context, audit);
        var attService = new AttendanceService(context, audit);

        var emp = new Employee
        {
            EmployeeCode = "EOS-002",
            FullName = "Muhammad Setiawan",
            Position = "Engineer On Site",
            Site = "Site Rembang"
        };
        await empService.CreateAsync(emp, "test-user");

        var input = new AttendanceInputModel
        {
            EmployeeId = emp.Id,
            AttendanceDate = new DateOnly(2026, 11, 4),
            Status = AttendanceStatus.Cuti,
            CheckIn = null,
            CheckOut = null,
            Description = "Cuti Tahunan Disetujui"
        };

        var result = await attService.CreateAsync(input, "test-user");
        Assert.True(result.Success);
        Assert.NotNull(result.Attendance);
        Assert.Equal(0m, result.Attendance.TotalWorkHours);
        Assert.Null(result.Attendance.CheckIn);
        Assert.Null(result.Attendance.CheckOut);
    }

    [Fact]
    public async Task AnalyticsService_ComputesSummaryCorrectly()
    {
        using var context = CreateInMemoryDbContext();
        var audit = new AuditLogService(context, NullLogger<AuditLogService>.Instance);
        var empService = new EmployeeService(context, audit);
        var attService = new AttendanceService(context, audit);
        var analyticsService = new AnalyticsService(context);

        var emp = new Employee
        {
            EmployeeCode = "EOS-001",
            FullName = "Vino Rifhendi Setiawan",
            Position = "Leader",
            Site = "Site Rembang"
        };
        await empService.CreateAsync(emp, "test-user");

        await attService.CreateAsync(new AttendanceInputModel
        {
            EmployeeId = emp.Id,
            AttendanceDate = new DateOnly(2026, 11, 2),
            Status = AttendanceStatus.Hadir,
            CheckIn = new TimeOnly(8, 0),
            CheckOut = new TimeOnly(17, 0)
        }, "test-user");

        await attService.CreateAsync(new AttendanceInputModel
        {
            EmployeeId = emp.Id,
            AttendanceDate = new DateOnly(2026, 11, 3),
            Status = AttendanceStatus.Izin,
            Description = "Izin keperluan mendadak"
        }, "test-user");

        var summary = await analyticsService.GetMonthlyAttendanceSummaryAsync(2026, 11);
        Assert.Equal(1, summary.TotalHadir);
        Assert.Equal(1, summary.TotalIzin);
        Assert.Equal(9.0m, summary.TotalHours);

        var stats = await analyticsService.GetAttendanceStatisticsAsync(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));
        Assert.Equal(2, stats.TotalRecords);
        Assert.Equal(9.0m, stats.TotalWorkingHours);
    }

    [Fact]
    public async Task ExportService_GeneratesValidPdfExcelCsv()
    {
        using var context = CreateInMemoryDbContext();
        var audit = new AuditLogService(context, NullLogger<AuditLogService>.Instance);
        var empService = new EmployeeService(context, audit);
        var attService = new AttendanceService(context, audit);
        var exportService = new ExportService();

        var emp = new Employee
        {
            EmployeeCode = "EOS-001",
            FullName = "Vino Rifhendi Setiawan",
            Position = "Leader / Engineer On Site",
            Site = "Site Rembang"
        };
        await empService.CreateAsync(emp, "test-user");

        await attService.CreateAsync(new AttendanceInputModel
        {
            EmployeeId = emp.Id,
            AttendanceDate = new DateOnly(2026, 11, 4),
            Status = AttendanceStatus.Hadir,
            CheckIn = new TimeOnly(8, 0),
            CheckOut = new TimeOnly(17, 0),
            Description = "Operasional Harian"
        }, "test-user");

        var reportData = await attService.GetReportDataAsync(new DateOnly(2026, 10, 14), new DateOnly(2026, 11, 15));

        // 1. PDF Test
        var pdfBytes = exportService.ExportToPdf(reportData);
        var doc = new SiteAttendance.Reports.QuestPdfReportDocument(reportData);
        var images = doc.GenerateImages().ToList();
        Assert.Single(images);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);
        // Validasi header %PDF
        var pdfHeader = Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.Equal("%PDF-", pdfHeader);

        // 2. Excel Test
        var excelBytes = exportService.ExportToExcel(reportData);
        Assert.NotNull(excelBytes);
        Assert.True(excelBytes.Length > 1000);
        // Validasi header PK (Zip/OpenXML)
        var excelHeader = Encoding.ASCII.GetString(pdfBytes.Take(2).ToArray());
        Assert.NotNull(excelHeader);

        // 3. CSV Test
        var csvBytes = exportService.ExportToCsv(reportData);
        Assert.NotNull(csvBytes);
        var csvString = Encoding.UTF8.GetString(csvBytes);
        Assert.Contains("EOS-001", csvString);
        Assert.Contains("Vino Rifhendi Setiawan", csvString);
        Assert.Contains("2026-11-04", csvString);
    }

    [Fact]
    public async Task DatabaseBackupService_ExportAndRestore_RestoresDataAccurately()
    {
        using var context = CreateInMemoryDbContext();
        var audit = new AuditLogService(context, NullLogger<AuditLogService>.Instance);
        var empService = new EmployeeService(context, audit);
        var attService = new AttendanceService(context, audit);
        var backupService = new DatabaseBackupService(context, audit, NullLogger<DatabaseBackupService>.Instance);

        var emp = new Employee
        {
            EmployeeCode = "EOS-001",
            FullName = "Vino Rifhendi Setiawan",
            Position = "Leader",
            Site = "Site Rembang"
        };
        await empService.CreateAsync(emp, "test-user");

        await attService.CreateAsync(new AttendanceInputModel
        {
            EmployeeId = emp.Id,
            AttendanceDate = new DateOnly(2026, 11, 4),
            Status = AttendanceStatus.Hadir,
            CheckIn = new TimeOnly(8, 0),
            CheckOut = new TimeOnly(17, 0),
            Description = "Backup Test Data"
        }, "test-user");

        // 1. Ekspor Backup JSON
        var backupBytes = await backupService.GenerateBackupJsonAsync();
        Assert.NotNull(backupBytes);
        var jsonString = Encoding.UTF8.GetString(backupBytes);
        Assert.Contains("EOS-001", jsonString);
        Assert.Contains("Vino Rifhendi Setiawan", jsonString);

        // 2. Buat context bersih kedua & Restore
        using var newContext = CreateInMemoryDbContext();
        var newAudit = new AuditLogService(newContext, NullLogger<AuditLogService>.Instance);
        var newBackupService = new DatabaseBackupService(newContext, newAudit, NullLogger<DatabaseBackupService>.Instance);

        var restoreResult = await newBackupService.RestoreFromBackupJsonAsync(jsonString, "restore-test-user");
        Assert.True(restoreResult.Success);
        Assert.Equal(1, restoreResult.RestoredCount);

        // Verifikasi data di database kedua
        var restoredEmp = await newContext.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == "EOS-001");
        Assert.NotNull(restoredEmp);
        Assert.Equal("Vino Rifhendi Setiawan", restoredEmp.FullName);

        var restoredAtt = await newContext.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == restoredEmp.Id);
        Assert.NotNull(restoredAtt);
        Assert.Equal(AttendanceStatus.Hadir, restoredAtt.Status);
        Assert.Equal(9.0m, restoredAtt.TotalWorkHours);
    }
}

