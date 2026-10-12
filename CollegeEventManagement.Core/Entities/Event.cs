namespace CollegeEventManagement.Core.Entities;

public class Event
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Technical, Cultural, Sports, Workshop, Seminar
    public string? ImageUrl { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Venue { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; } = true;

    // Navigation properties
    public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
