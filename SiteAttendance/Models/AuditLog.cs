using System.ComponentModel.DataAnnotations;

namespace SiteAttendance.Models;

public class AuditLog
{
    public int Id { get; set; }

    [MaxLength(100)]
    [Display(Name = "ID / Nama User")]
    public string? UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Aksi")]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Entitas")]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "ID Entitas")]
    public string? EntityId { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Deskripsi / Rincian")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Waktu Kejadian (UTC)")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

