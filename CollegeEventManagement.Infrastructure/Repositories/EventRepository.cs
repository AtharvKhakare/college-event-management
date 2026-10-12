using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Core.Entities;
using CollegeEventManagement.Core.Interfaces;
using CollegeEventManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CollegeEventManagement.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.EventId == id);
    }

    public async Task<EventDto?> GetEventDtoByIdAsync(int id, string? currentUserId = null)
    {
        var ev = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null) return null;

        var userReg = !string.IsNullOrEmpty(currentUserId)
            ? ev.Registrations.FirstOrDefault(r => r.UserId == currentUserId && r.Status != RegistrationStatus.Cancelled)
            : null;

        return new EventDto
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
            RegisteredCount = ev.Registrations.Count(r => r.Status != RegistrationStatus.Cancelled && r.Status != RegistrationStatus.Rejected),
            IsUserRegistered = userReg != null,
            UserRegistrationStatus = userReg?.Status,
            UserRegistrationId = userReg?.RegistrationId
        };
    }

    public async Task<PagedResult<EventDto>> GetAllAsync(EventFilterDto filter, string? currentUserId = null)
    {
        var query = _context.Events.AsQueryable();

        if (filter.OnlyPublished.HasValue && filter.OnlyPublished.Value)
        {
            query = query.Where(e => e.IsPublished);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(search) || 
                                     e.Description.ToLower().Contains(search) || 
                                     e.Venue.ToLower().Contains(search) ||
                                     e.OrganizerName.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category) && filter.Category != "All")
        {
            query = query.Where(e => e.Category == filter.Category);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(e => e.EventDate.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(e => e.EventDate.Date <= filter.ToDate.Value.Date);
        }

        var totalCount = await query.CountAsync();

        var events = await query
            .Include(e => e.Registrations)
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.StartTime)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = events.Select(ev =>
        {
            var userReg = !string.IsNullOrEmpty(currentUserId)
                ? ev.Registrations.FirstOrDefault(r => r.UserId == currentUserId && r.Status != RegistrationStatus.Cancelled)
                : null;

            return new EventDto
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
                RegisteredCount = ev.Registrations.Count(r => r.Status != RegistrationStatus.Cancelled && r.Status != RegistrationStatus.Rejected),
                IsUserRegistered = userReg != null,
                UserRegistrationStatus = userReg?.Status,
                UserRegistrationId = userReg?.RegistrationId
            };
        }).ToList();

        return new PagedResult<EventDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<Event> AddAsync(Event ev)
    {
        _context.Events.Add(ev);
        await _context.SaveChangesAsync();
        return ev;
    }

    public async Task UpdateAsync(Event ev)
    {
        _context.Entry(ev).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev != null)
        {
            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetRegisteredCountAsync(int eventId)
    {
        return await _context.EventRegistrations
            .Where(r => r.EventId == eventId && r.Status != RegistrationStatus.Cancelled && r.Status != RegistrationStatus.Rejected)
            .CountAsync();
    }

    public async Task<bool> IsUserRegisteredAsync(int eventId, string userId)
    {
        return await _context.EventRegistrations
            .AnyAsync(r => r.EventId == eventId && r.UserId == userId && r.Status != RegistrationStatus.Cancelled);
    }
}
