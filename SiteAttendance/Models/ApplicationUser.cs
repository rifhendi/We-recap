using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SiteAttendance.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

