using System.ComponentModel.DataAnnotations;

namespace SiteAttendance.Models;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Kode Engineer")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Display(Name = "Nama Lengkap")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Jabatan")]
    public string Position { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Lokasi Site")]
    public string Site { get; set; } = "Site Rembang";

    [Display(Name = "Status Aktif")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Dibuat Pada")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}

