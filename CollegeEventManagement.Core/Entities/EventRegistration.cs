using CollegeEventManagement.Core.Constants;

namespace CollegeEventManagement.Core.Entities;

public class EventRegistration
{
    public int RegistrationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int EventId { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = RegistrationStatus.Approved;
    public string AttendanceStatus { get; set; } = AttendanceStatuses.Unmarked;

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual Event? Event { get; set; }
}
