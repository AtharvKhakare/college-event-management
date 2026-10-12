namespace CollegeEventManagement.Core.DTOs;

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class AdminDashboardDto
{
    public int TotalEvents { get; set; }
    public int TotalStudents { get; set; }
    public int TotalRegistrations { get; set; }
    public int UpcomingEventsCount { get; set; }

    public double AttendanceRate { get; set; } // percentage
    public int TotalPresent { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalUnmarked { get; set; }

    public List<EventDto> UpcomingEvents { get; set; } = new();
    public List<RegistrationDto> RecentRegistrations { get; set; } = new();

    public List<ChartDataPoint> MonthlyRegistrations { get; set; } = new();
    public List<ChartDataPoint> CategoryDistribution { get; set; } = new();
    public List<ChartDataPoint> AttendanceBreakdown { get; set; } = new();
}

public class StudentDashboardDto
{
    public string StudentName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? Department { get; set; }
    public string? AcademicYear { get; set; }

    public int TotalRegisteredEvents { get; set; }
    public int UpcomingEventsCount { get; set; }
    public int PastEventsCount { get; set; }
    public int AttendedEventsCount { get; set; }

    public List<RegistrationDto> UpcomingRegistrations { get; set; } = new();
    public List<RegistrationDto> PastRegistrations { get; set; } = new();
    public List<RegistrationDto> RecentRegistrations { get; set; } = new();
}

public class StudentAccountDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? Department { get; set; }
    public string? AcademicYear { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RegistrationCount { get; set; }
}
