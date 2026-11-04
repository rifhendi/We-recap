using System.ComponentModel.DataAnnotations;
using SiteAttendance.Models;

namespace SiteAttendance.ViewModels;

public class AttendanceFilterViewModel
{
    [Display(Name = "Dari Tanggal")]
    public DateOnly? StartDate { get; set; }

    [Display(Name = "Sampai Tanggal")]
    public DateOnly? EndDate { get; set; }

    [Display(Name = "Pilih Engineer")]
    public int? EmployeeId { get; set; }

    [Display(Name = "Status Kehadiran")]
    public AttendanceStatus? Status { get; set; }

    [Display(Name = "Pencarian")]
    public string? Search { get; set; }
}

public class AttendanceInputModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Pilih engineer terlebih dahulu.")]
    [Display(Name = "Engineer")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Tanggal absensi wajib diisi.")]
    [Display(Name = "Tanggal Absensi")]
    public DateOnly AttendanceDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)); // UTC+7 Jakarta

    [Display(Name = "Jam Masuk (HH:mm)")]
    public TimeOnly? CheckIn { get; set; }

    [Display(Name = "Jam Pulang (HH:mm)")]
    public TimeOnly? CheckOut { get; set; }

    [Required(ErrorMessage = "Status kehadiran wajib dipilih.")]
    [Display(Name = "Status Kehadiran")]
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Hadir;

    [MaxLength(500, ErrorMessage = "Keterangan maksimal 500 karakter.")]
    [Display(Name = "Keterangan (Opsional)")]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status == AttendanceStatus.Hadir)
        {
            if (CheckIn == null)
            {
                yield return new ValidationResult("Jam Masuk wajib diisi untuk status Hadir.", new[] { nameof(CheckIn) });
            }

            if (CheckOut == null)
            {
                yield return new ValidationResult("Jam Pulang wajib diisi untuk status Hadir.", new[] { nameof(CheckOut) });
            }

            if (CheckIn.HasValue && CheckOut.HasValue)
            {
                if (CheckOut.Value < CheckIn.Value)
                {
                    yield return new ValidationResult("Jam Pulang tidak boleh lebih awal dari Jam Masuk.", new[] { nameof(CheckOut) });
                }
            }
        }
    }

    public decimal CalculateTotalWorkHours()
    {
        if (Status != AttendanceStatus.Hadir || !CheckIn.HasValue || !CheckOut.HasValue)
        {
            return 0;
        }

        var duration = CheckOut.Value - CheckIn.Value;
        var hours = (decimal)duration.TotalHours;
        return Math.Round(hours >= 0 ? hours : 0, 2);
    }
}

