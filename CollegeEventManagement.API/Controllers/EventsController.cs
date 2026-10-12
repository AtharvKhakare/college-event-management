using System.Security.Claims;
using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Core.Entities;
using CollegeEventManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventRepository _eventRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventRepository eventRepository,
        IWebHostEnvironment environment,
        ILogger<EventsController> logger)
    {
        _eventRepository = eventRepository;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EventDto>>>> GetAll([FromQuery] EventFilterDto filter)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // If user is Admin, they might view unpublished events if requested
        var isAdmin = User.IsInRole(AppRoles.Admin);
        if (!isAdmin)
        {
            filter.OnlyPublished = true;
        }

        var result = await _eventRepository.GetAllAsync(filter, currentUserId);
        return Ok(ApiResponse<PagedResult<EventDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<EventDto>>> GetById(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ev = await _eventRepository.GetEventDtoByIdAsync(id, currentUserId);

        if (ev == null)
        {
            return NotFound(ApiResponse<EventDto>.Fail("Event not found."));
        }

        var isAdmin = User.IsInRole(AppRoles.Admin);
        if (!ev.IsPublished && !isAdmin)
        {
            return NotFound(ApiResponse<EventDto>.Fail("Event is currently not published."));
        }

        return Ok(ApiResponse<EventDto>.Ok(ev));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<EventDto>>> Create([FromBody] CreateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<EventDto>.Fail("Validation failed", errors));
        }

        var ev = new Event
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Category = dto.Category.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? "/images/events/default-event.jpg" : dto.ImageUrl.Trim(),
            EventDate = dto.EventDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Venue = dto.Venue.Trim(),
            Capacity = dto.Capacity,
            RegistrationDeadline = dto.RegistrationDeadline,
            OrganizerName = dto.OrganizerName.Trim(),
            IsPublished = dto.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _eventRepository.AddAsync(ev);
        var createdDto = await _eventRepository.GetEventDtoByIdAsync(created.EventId);

        _logger.LogInformation("Admin created event: {Title} (ID: {Id})", ev.Title, ev.EventId);
        return CreatedAtAction(nameof(GetById), new { id = created.EventId }, ApiResponse<EventDto>.Ok(createdDto!, "Event created successfully."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<EventDto>>> Update(int id, [FromBody] UpdateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<EventDto>.Fail("Validation failed", errors));
        }

        var ev = await _eventRepository.GetByIdAsync(id);
        if (ev == null)
        {
            return NotFound(ApiResponse<EventDto>.Fail("Event not found."));
        }

        // Validate that capacity is not lowered below active registered count
        var activeCount = await _eventRepository.GetRegisteredCountAsync(id);
        if (dto.Capacity < activeCount)
        {
            return BadRequest(ApiResponse<EventDto>.Fail($"Event capacity cannot be less than the current active registrations count ({activeCount})."));
        }

        ev.Title = dto.Title.Trim();
        ev.Description = dto.Description.Trim();
        ev.Category = dto.Category.Trim();
        if (!string.IsNullOrWhiteSpace(dto.ImageUrl)) ev.ImageUrl = dto.ImageUrl.Trim();
        ev.EventDate = dto.EventDate;
        ev.StartTime = dto.StartTime;
        ev.EndTime = dto.EndTime;
        ev.Venue = dto.Venue.Trim();
        ev.Capacity = dto.Capacity;
        ev.RegistrationDeadline = dto.RegistrationDeadline;
        ev.OrganizerName = dto.OrganizerName.Trim();
        ev.IsPublished = dto.IsPublished;

        await _eventRepository.UpdateAsync(ev);
        var updatedDto = await _eventRepository.GetEventDtoByIdAsync(id);

        _logger.LogInformation("Admin updated event ID: {Id}", id);
        return Ok(ApiResponse<EventDto>.Ok(updatedDto!, "Event updated successfully."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var ev = await _eventRepository.GetByIdAsync(id);
        if (ev == null)
        {
            return NotFound(ApiResponse<bool>.Fail("Event not found."));
        }

        await _eventRepository.DeleteAsync(id);
        _logger.LogInformation("Admin deleted event ID: {Id}", id);

        return Ok(ApiResponse<bool>.Ok(true, "Event deleted successfully."));
    }

    [HttpPost("upload-image")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<string>>> UploadImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<string>.Fail("No image file provided."));
        }

        // Limit size to 5MB
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(ApiResponse<string>.Fail("Image size exceeds the 5MB limit."));
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(ApiResponse<string>.Fail("Invalid image format. Allowed formats: JPG, JPEG, PNG, WEBP."));
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "events");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/events/{uniqueFileName}";
        return Ok(ApiResponse<string>.Ok(relativeUrl, "Image uploaded successfully."));
    }
}
