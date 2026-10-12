using System.ComponentModel.DataAnnotations;
using CollegeEventManagement.Core.Constants;

namespace CollegeEventManagement.Core.DTOs;

public class CreateRegistrationDto
{
    [Required(ErrorMessage = "Event ID is required.")]
    public int EventId { get; set; }
}

public class RegistrationDto
{
    public int RegistrationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? Department { get; set; }
    public string? AcademicYear { get; set; }

    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public string? EventImageUrl { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Venue { get; set; } = string.Empty;

    public DateTime RegistrationDate { get; set; }
    public string Status { get; set; } = RegistrationStatus.Approved;
    public string AttendanceStatus { get; set; } = AttendanceStatuses.Unmarked;

    public bool IsPastEvent => DateTime.UtcNow.Date > EventDate.Date;
    public bool CanCancel => Status == RegistrationStatus.Approved && !IsPastEvent;
}

public class UpdateRegistrationStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}

public class UpdateAttendanceDto
{
    [Required(ErrorMessage = "Attendance status is required.")]
    public string AttendanceStatus { get; set; } = string.Empty;
}

public class RegistrationFilterDto
{
    public int? EventId { get; set; }
    public string? Status { get; set; }
    public string? AttendanceStatus { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
