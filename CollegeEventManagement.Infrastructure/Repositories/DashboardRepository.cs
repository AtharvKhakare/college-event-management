using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Core.Entities;
using CollegeEventManagement.Core.Interfaces;
using CollegeEventManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CollegeEventManagement.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardDataAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var totalEvents = await _context.Events.CountAsync();
        
        // Count students in Student role
        var studentUsers = await _userManager.GetUsersInRoleAsync(AppRoles.Student);
        var totalStudents = studentUsers.Count;

        var allRegistrations = await _context.EventRegistrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .ToListAsync();

        var totalRegistrations = allRegistrations.Count(r => r.Status != RegistrationStatus.Cancelled);
        var upcomingEventsCount = await _context.Events.CountAsync(e => e.EventDate.Date >= today && e.IsPublished);

        var presentCount = allRegistrations.Count(r => r.AttendanceStatus == AttendanceStatuses.Present);
        var absentCount = allRegistrations.Count(r => r.AttendanceStatus == AttendanceStatuses.Absent);
        var unmarkedCount = allRegistrations.Count(r => r.AttendanceStatus == AttendanceStatuses.Unmarked && r.Status == RegistrationStatus.Approved);

        var attendedOrAbsent = presentCount + absentCount;
        var attendanceRate = attendedOrAbsent > 0 ? Math.Round((double)presentCount / attendedOrAbsent * 100, 1) : 0;

        // Upcoming events
        var upcomingEventsEntities = await _context.Events
            .Include(e => e.Registrations)
            .Where(e => e.EventDate.Date >= today && e.IsPublished)
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.StartTime)
            .Take(5)
            .ToListAsync();

        var upcomingEventsDto = upcomingEventsEntities.Select(ev => new EventDto
        {
            EventId = ev.EventId,
            Title = ev.Title,
            Description = ev.Description,
            Category = ev.Category,
            ImageUrl = ev.ImageUrl,
            EventDate = ev.EventDate,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
            Venue = ev.Venue,
            Capacity = ev.Capacity,
            RegistrationDeadline = ev.RegistrationDeadline,
            OrganizerName = ev.OrganizerName,
            CreatedAt = ev.CreatedAt,
            IsPublished = ev.IsPublished,
            RegisteredCount = ev.Registrations.Count(r => r.Status != RegistrationStatus.Cancelled && r.Status != RegistrationStatus.Rejected)
        }).ToList();

        // Recent registrations
        var recentRegistrationsDto = allRegistrations
            .OrderByDescending(r => r.RegistrationDate)
            .Take(5)
            .Select(r => new RegistrationDto
            {
                RegistrationId = r.RegistrationId,
                UserId = r.UserId,
                StudentName = r.User?.FullName ?? "Unknown",
                StudentEmail = r.User?.Email ?? string.Empty,
                StudentId = r.User?.StudentId,
                Department = r.User?.Department,
                AcademicYear = r.User?.AcademicYear,
                EventId = r.EventId,
                EventTitle = r.Event?.Title ?? "Unknown Event",
                EventCategory = r.Event?.Category ?? string.Empty,
                EventImageUrl = r.Event?.ImageUrl,
                EventDate = r.Event?.EventDate ?? DateTime.MinValue,
                StartTime = r.Event?.StartTime ?? TimeSpan.Zero,
                EndTime = r.Event?.EndTime ?? TimeSpan.Zero,
                Venue = r.Event?.Venue ?? string.Empty,
                RegistrationDate = r.RegistrationDate,
                Status = r.Status,
                AttendanceStatus = r.AttendanceStatus
            }).ToList();

        // Monthly registrations (last 6 months)
        var monthlyList = new List<ChartDataPoint>();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var label = targetMonth.ToString("MMM yyyy");
            var count = allRegistrations.Count(r => 
                r.RegistrationDate.Year == targetMonth.Year && 
                r.RegistrationDate.Month == targetMonth.Month &&
                r.Status != RegistrationStatus.Cancelled);
            monthlyList.Add(new ChartDataPoint { Label = label, Value = count });
        }

        // Category distribution
        var categoryPoints = EventCategories.All.Select(cat => new ChartDataPoint
        {
            Label = cat,
            Value = allRegistrations.Count(r => r.Event?.Category == cat && r.Status != RegistrationStatus.Cancelled)
        }).ToList();

        // Attendance breakdown
        var attendancePoints = new List<ChartDataPoint>
        {
            new() { Label = "Present", Value = presentCount },
            new() { Label = "Absent", Value = absentCount },
            new() { Label = "Unmarked", Value = unmarkedCount }
        };

        return new AdminDashboardDto
        {
            TotalEvents = totalEvents,
            TotalStudents = totalStudents,
            TotalRegistrations = totalRegistrations,
            UpcomingEventsCount = upcomingEventsCount,
            AttendanceRate = attendanceRate,
            TotalPresent = presentCount,
            TotalAbsent = absentCount,
            TotalUnmarked = unmarkedCount,
            UpcomingEvents = upcomingEventsDto,
            RecentRegistrations = recentRegistrationsDto,
            MonthlyRegistrations = monthlyList,
            CategoryDistribution = categoryPoints,
            AttendanceBreakdown = attendancePoints
        };
    }

    public async Task<StudentDashboardDto> GetStudentDashboardDataAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var today = DateTime.UtcNow.Date;

        var registrations = await _context.EventRegistrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RegistrationDate)
            .ToListAsync();

        var activeRegs = registrations.Where(r => r.Status != RegistrationStatus.Cancelled).ToList();

        var upcomingRegs = activeRegs
            .Where(r => r.Event != null && r.Event.EventDate.Date >= today)
            .OrderBy(r => r.Event!.EventDate)
            .ThenBy(r => r.Event!.StartTime)
            .Select(MapToRegistrationDto)
            .ToList();

        var pastRegs = activeRegs
            .Where(r => r.Event != null && r.Event.EventDate.Date < today)
            .OrderByDescending(r => r.Event!.EventDate)
            .Select(MapToRegistrationDto)
            .ToList();

        var recentRegs = registrations
            .Take(5)
            .Select(MapToRegistrationDto)
            .ToList();

        return new StudentDashboardDto
        {
            StudentName = user?.FullName ?? "Student",
            Email = user?.Email ?? string.Empty,
            StudentId = user?.StudentId,
            Department = user?.Department,
            AcademicYear = user?.AcademicYear,
            TotalRegisteredEvents = activeRegs.Count,
            UpcomingEventsCount = upcomingRegs.Count,
            PastEventsCount = pastRegs.Count,
            AttendedEventsCount = activeRegs.Count(r => r.AttendanceStatus == AttendanceStatuses.Present),
            UpcomingRegistrations = upcomingRegs,
            PastRegistrations = pastRegs,
            RecentRegistrations = recentRegs
        };
    }

    public async Task<PagedResult<StudentAccountDto>> GetStudentsListAsync(string? search, int pageNumber = 1, int pageSize = 10)
    {
        var studentUsers = await _userManager.GetUsersInRoleAsync(AppRoles.Student);
        var query = studentUsers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) ||
                                     (u.Email != null && u.Email.ToLower().Contains(term)) ||
                                     (u.StudentId != null && u.StudentId.ToLower().Contains(term)) ||
                                     (u.Department != null && u.Department.ToLower().Contains(term)));
        }

        var totalCount = query.Count();
        var students = query
            .OrderBy(u => u.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var userIds = students.Select(s => s.Id).ToList();
        var regCounts = await _context.EventRegistrations
            .Where(r => userIds.Contains(r.UserId) && r.Status != RegistrationStatus.Cancelled)
            .GroupBy(r => r.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Count);

        var items = students.Select(s => new StudentAccountDto
        {
            Id = s.Id,
            FullName = s.FullName,
            Email = s.Email ?? string.Empty,
            StudentId = s.StudentId,
            Department = s.Department,
            AcademicYear = s.AcademicYear,
            CreatedAt = s.CreatedAt,
            RegistrationCount = regCounts.TryGetValue(s.Id, out var count) ? count : 0
        }).ToList();

        return new PagedResult<StudentAccountDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static RegistrationDto MapToRegistrationDto(EventRegistration r)
    {
        return new RegistrationDto
        {
            RegistrationId = r.RegistrationId,
            UserId = r.UserId,
            StudentName = r.User?.FullName ?? "Unknown",
            StudentEmail = r.User?.Email ?? string.Empty,
            StudentId = r.User?.StudentId,
            Department = r.User?.Department,
            AcademicYear = r.User?.AcademicYear,
            EventId = r.EventId,
            EventTitle = r.Event?.Title ?? "Unknown Event",
            EventCategory = r.Event?.Category ?? string.Empty,
            EventImageUrl = r.Event?.ImageUrl,
            EventDate = r.Event?.EventDate ?? DateTime.MinValue,
            StartTime = r.Event?.StartTime ?? TimeSpan.Zero,
            EndTime = r.Event?.EndTime ?? TimeSpan.Zero,
            Venue = r.Event?.Venue ?? string.Empty,
            RegistrationDate = r.RegistrationDate,
            Status = r.Status,
            AttendanceStatus = r.AttendanceStatus
        };
    }
}
