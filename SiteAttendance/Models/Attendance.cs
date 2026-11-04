using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SiteAttendance.Models;

public class Attendance
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Engineer")]
    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    [Required]
    [Display(Name = "Tanggal Absensi")]
    public DateOnly AttendanceDate { get; set; }

    [Display(Name = "Jam Masuk")]
    public TimeOnly? CheckIn { get; set; }

    [Display(Name = "Jam Pulang")]
    public TimeOnly? CheckOut { get; set; }

    [Required]
    [Display(Name = "Status Kehadiran")]
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Hadir;

    [MaxLength(500)]
    [Display(Name = "Keterangan")]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    [Display(Name = "Total Jam Kerja")]
    public decimal TotalWorkHours { get; set; } = 0;

    [Display(Name = "Dibuat Pada")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Diperbarui Pada")]
    public DateTime? UpdatedAt { get; set; }
}

