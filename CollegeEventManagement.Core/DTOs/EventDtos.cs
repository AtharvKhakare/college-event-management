using System.ComponentModel.DataAnnotations;

namespace CollegeEventManagement.Core.DTOs;

public class EventDto
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Venue { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPublished { get; set; }

    // Computed fields
    public int RegisteredCount { get; set; }
    public int RemainingCapacity => Math.Max(0, Capacity - RegisteredCount);
    public bool IsFull => RegisteredCount >= Capacity;
    public bool IsDeadlinePassed => DateTime.UtcNow > RegistrationDeadline;
    public bool IsPastEvent => DateTime.UtcNow.Date > EventDate.Date;
    public bool IsRegistrationOpen => IsPublished && !IsPastEvent && !IsDeadlinePassed && !IsFull;
    public bool IsUserRegistered { get; set; }
    public string? UserRegistrationStatus { get; set; }
    public int? UserRegistrationId { get; set; }
}

public class CreateEventDto : IValidatableObject
{
    [Required(ErrorMessage = "Event title is required.")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Event description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Event category is required.")]
    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
    public string Category { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Event date is required.")]
    [DataType(DataType.Date)]
    public DateTime EventDate { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    public TimeSpan StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public TimeSpan EndTime { get; set; }

    [Required(ErrorMessage = "Venue is required.")]
    [StringLength(200, ErrorMessage = "Venue cannot exceed 200 characters.")]
    public string Venue { get; set; } = string.Empty;

    [Required(ErrorMessage = "Capacity is required.")]
    [Range(1, 100000, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Registration deadline is required.")]
    [DataType(DataType.DateTime)]
    public DateTime RegistrationDeadline { get; set; }

    [Required(ErrorMessage = "Organizer name is required.")]
    [StringLength(150, ErrorMessage = "Organizer name cannot exceed 150 characters.")]
    public string OrganizerName { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("End time must be after start time.", [nameof(EndTime)]);
        }

        if (RegistrationDeadline.Date > EventDate.Date)
        {
            yield return new ValidationResult("Registration deadline cannot be after the event date.", [nameof(RegistrationDeadline)]);
        }
    }
}

public class UpdateEventDto : CreateEventDto
{
}

public class EventFilterDto
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? OnlyPublished { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 9;
}
