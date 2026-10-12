using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Core.Entities;
using CollegeEventManagement.Core.Interfaces;
using CollegeEventManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CollegeEventManagement.Infrastructure.Repositories;

public class RegistrationRepository : IRegistrationRepository
{
    private readonly ApplicationDbContext _context;

    public RegistrationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EventRegistration?> GetByIdAsync(int id)
    {
        return await _context.EventRegistrations
            .Include(r => r.User)
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.RegistrationId == id);
    }

    public async Task<RegistrationDto?> GetRegistrationDtoByIdAsync(int id)
    {
        var reg = await _context.EventRegistrations
            .Include(r => r.User)
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.RegistrationId == id);

        if (reg == null) return null;

        return MapToDto(reg);
    }

    public async Task<List<RegistrationDto>> GetUserRegistrationsAsync(string userId)
    {
        var registrations = await _context.EventRegistrations
            .Include(r => r.User)
            .Include(r => r.Event)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RegistrationDate)
            .ToListAsync();

        return registrations.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<RegistrationDto>> GetAllAsync(RegistrationFilterDto filter)
    {
        var query = _context.EventRegistrations
            .Include(r => r.User)
            .Include(r => r.Event)
            .AsQueryable();

        if (filter.EventId.HasValue && filter.EventId.Value > 0)
        {
            query = query.Where(r => r.EventId == filter.EventId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "All")
        {
            query = query.Where(r => r.Status == filter.Status);
        }

        if (!string.IsNullOrWhiteSpace(filter.AttendanceStatus) && filter.AttendanceStatus != "All")
        {
            query = query.Where(r => r.AttendanceStatus == filter.AttendanceStatus);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(r => 
                (r.User != null && (r.User.FullName.ToLower().Contains(search) || 
                                    (r.User.StudentId != null && r.User.StudentId.ToLower().Contains(search)) || 
                                    (r.User.Email != null && r.User.Email.ToLower().Contains(search)))) ||
                (r.Event != null && r.Event.Title.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.RegistrationDate)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<RegistrationDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<EventRegistration> AddAsync(EventRegistration registration)
    {
        _context.EventRegistrations.Add(registration);
        await _context.SaveChangesAsync();
        return registration;
    }

    public async Task UpdateAsync(EventRegistration registration)
    {
        _context.Entry(registration).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var reg = await _context.EventRegistrations.FindAsync(id);
        if (reg != null)
        {
            _context.EventRegistrations.Remove(reg);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<EventRegistration?> GetDuplicateRegistrationAsync(int eventId, string userId)
    {
        return await _context.EventRegistrations
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
    }

    public async Task<List<RegistrationDto>> GetExportRegistrationsAsync(int? eventId, string? status, string? attendanceStatus)
    {
        var query = _context.EventRegistrations
            .Include(r => r.User)
            .Include(r => r.Event)
            .AsQueryable();

        if (eventId.HasValue && eventId.Value > 0)
        {
            query = query.Where(r => r.EventId == eventId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(attendanceStatus) && attendanceStatus != "All")
        {
            query = query.Where(r => r.AttendanceStatus == attendanceStatus);
        }

        var items = await query
            .OrderBy(r => r.Event != null ? r.Event.Title : "")
            .ThenBy(r => r.RegistrationDate)
            .ToListAsync();

        return items.Select(MapToDto).ToList();
    }

    private static RegistrationDto MapToDto(EventRegistration reg)
    {
        return new RegistrationDto
        {
            RegistrationId = reg.RegistrationId,
            UserId = reg.UserId,
            StudentName = reg.User?.FullName ?? "Unknown",
            StudentEmail = reg.User?.Email ?? string.Empty,
            StudentId = reg.User?.StudentId,
            Department = reg.User?.Department,
            AcademicYear = reg.User?.AcademicYear,
            EventId = reg.EventId,
            EventTitle = reg.Event?.Title ?? "Unknown Event",
            EventCategory = reg.Event?.Category ?? string.Empty,
            EventImageUrl = reg.Event?.ImageUrl,
            EventDate = reg.Event?.EventDate ?? DateTime.MinValue,
            StartTime = reg.Event?.StartTime ?? TimeSpan.Zero,
            EndTime = reg.Event?.EndTime ?? TimeSpan.Zero,
            Venue = reg.Event?.Venue ?? string.Empty,
            RegistrationDate = reg.RegistrationDate,
            Status = reg.Status,
            AttendanceStatus = reg.AttendanceStatus
        };
    }
}
