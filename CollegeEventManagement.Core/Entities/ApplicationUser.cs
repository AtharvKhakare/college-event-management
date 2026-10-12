using Microsoft.AspNetCore.Identity;

namespace CollegeEventManagement.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? Department { get; set; }
    public string? AcademicYear { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
