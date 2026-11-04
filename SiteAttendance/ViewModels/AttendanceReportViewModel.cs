using System.Globalization;
using SiteAttendance.Models;

namespace SiteAttendance.ViewModels;

public class AttendanceReportViewModel
{
    public string Title { get; set; } = "ABSENSI ENGINEER ON SITE";
    public string Subtitle { get; set; } = "MANAGE SERVICE SIG";
    public string Location { get; set; } = "Site Rembang";

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string PeriodText
    {
        get
        {
            var culture = new CultureInfo("id-ID");
            return $"{StartDate.ToString("d MMMM yyyy", culture)} - {EndDate.ToString("d MMMM yyyy", culture)}";
        }
    }

    // Filter target
    public int? EmployeeId { get; set; }
    public Employee? SelectedEmployee { get; set; }

    // Report items per engineer
    public List<EmployeeReportSection> Sections { get; set; } = new();

    // Summary numbers overall
    public int TotalHadir { get; set; }
    public int TotalIzin { get; set; }
    public int TotalSakit { get; set; }
    public int TotalCuti { get; set; }
    public int TotalLibur { get; set; }
    public decimal TotalWorkHours { get; set; }

    // Formal Signatures Default
    public SignatureBlock CreatedBy { get; set; } = new()
    {
        Header = "Dibuat oleh,",
        Company = "PT Kirana Sakti Komputindo",
        Name = "Vino Rifhendi Setiawan",
        Title = "Leader / Engineer on Site Rembang"
    };

    public SignatureBlock AcknowledgedBy { get; set; } = new()
    {
        Header = "Mengetahui,",
        Company = "PT Kirana Sakti Komputindo",
        Name = "Dina Frinda Saragih",
        Title = "Project Manager MS WiFi"
    };

    public SignatureBlock ApprovedBy { get; set; } = new()
    {
        Header = "Menyetujui,",
        Company = "PT Semen Gresik (Persero) Tbk",
        Name = "Dwi Mulyantoro",
        Title = "IT Site Infrastructure Officer"
    };
}

public class SignatureBlock
{
    public string Header { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class EmployeeReportSection
{
    public Employee Employee { get; set; } = null!;
    public List<AttendanceReportRow> Rows { get; set; } = new();
    public int HadirCount { get; set; }
    public int IzinCount { get; set; }
    public int SakitCount { get; set; }
    public int CutiCount { get; set; }
    public int LiburCount { get; set; }
    public decimal TotalHours { get; set; }

    // Dedicated signature for this section's engineer
    public SignatureBlock CreatedBy { get; set; } = new();
}

public class AttendanceReportRow
{
    public int No { get; set; }
    public DateOnly Date { get; set; }
    public string FormattedDate => Date.ToString("d MMMM yyyy", new CultureInfo("id-ID"));
    public string DayName { get; set; } = string.Empty;
    public string CheckInText { get; set; } = "-";
    public string CheckOutText { get; set; } = "-";
    public AttendanceStatus Status { get; set; }
    public decimal WorkHours { get; set; }
    public string Description { get; set; } = string.Empty;

    public static string GetIndonesianDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => "Minggu",
        DayOfWeek.Monday => "Senin",
        DayOfWeek.Tuesday => "Selasa",
        DayOfWeek.Wednesday => "Rabu",
        DayOfWeek.Thursday => "Kamis",
        DayOfWeek.Friday => "Jumat",
        DayOfWeek.Saturday => "Sabtu",
        _ => day.ToString()
    };
}
