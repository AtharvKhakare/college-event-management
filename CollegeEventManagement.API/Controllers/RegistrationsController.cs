using System.Security.Claims;
using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Core.Entities;
using CollegeEventManagement.Core.Interfaces;
using CollegeEventManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeEventManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RegistrationsController> _logger;

    public RegistrationsController(
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        ApplicationDbContext context,
        ILogger<RegistrationsController> logger)
    {
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<ApiResponse<RegistrationDto>>> Register([FromBody] CreateRegistrationDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<RegistrationDto>.Fail("Please log in to register."));
        }

        // Fetch event
        var ev = await _eventRepository.GetByIdAsync(dto.EventId);
        if (ev == null)
        {
            return NotFound(ApiResponse<RegistrationDto>.Fail("Event not found."));
        }

        // Business Rule 2: Students cannot register for unpublished events
        if (!ev.IsPublished)
        {
            return BadRequest(ApiResponse<RegistrationDto>.Fail("This event is not published or currently unavailable for registration."));
        }

        var now = DateTime.UtcNow;

        // Business Rule 3: Students cannot register after the registration deadline
        if (now > ev.RegistrationDeadline)
        {
            return BadRequest(ApiResponse<RegistrationDto>.Fail("Registration deadline has passed."));
        }

        // Business Rule 4: Students cannot register for past events
        if (now.Date > ev.EventDate.Date)
        {
            return BadRequest(ApiResponse<RegistrationDto>.Fail("Cannot register for past events."));
        }

        // Concurrency-safe registration execution using a database transaction with serializable isolation or atomic check
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Business Rule 6: Duplicate registrations must be prevented
            var existingReg = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == dto.EventId && r.UserId == userId);

            if (existingReg != null)
            {
                if (existingReg.Status != RegistrationStatus.Cancelled)
                {
                    return BadRequest(ApiResponse<RegistrationDto>.Fail("You have already registered for this event."));
                }
                else
                {
                    // If previously cancelled, check capacity before re-activating
                    var currentCount = await _context.EventRegistrations
                        .CountAsync(r => r.EventId == dto.EventId && 
                                         r.Status != RegistrationStatus.Cancelled && 
                                         r.Status != RegistrationStatus.Rejected);

                    if (currentCount >= ev.Capacity)
                    {
                        return BadRequest(ApiResponse<RegistrationDto>.Fail("Sorry, this event is fully booked."));
                    }

                    existingReg.Status = RegistrationStatus.Approved;
                    existingReg.RegistrationDate = DateTime.UtcNow;
                    existingReg.AttendanceStatus = AttendanceStatuses.Unmarked;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var reResultDto = await _registrationRepository.GetRegistrationDtoByIdAsync(existingReg.RegistrationId);
                    return Ok(ApiResponse<RegistrationDto>.Ok(reResultDto!, "Registration successful! Welcome back."));
                }
            }

            // Business Rule 5 & 8: Check capacity
            var activeCount = await _context.EventRegistrations
                .CountAsync(r => r.EventId == dto.EventId && 
                                 r.Status != RegistrationStatus.Cancelled && 
                                 r.Status != RegistrationStatus.Rejected);

            if (activeCount >= ev.Capacity)
            {
                return BadRequest(ApiResponse<RegistrationDto>.Fail("Sorry, this event is fully booked."));
            }

            // Create new registration
            var registration = new EventRegistration
            {
                UserId = userId,
                EventId = dto.EventId,
                RegistrationDate = DateTime.UtcNow,
                Status = RegistrationStatus.Approved,
                AttendanceStatus = AttendanceStatuses.Unmarked
            };

            _context.EventRegistrations.Add(registration);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Student {UserId} registered for Event {EventId}", userId, dto.EventId);

            var resultDto = await _registrationRepository.GetRegistrationDtoByIdAsync(registration.RegistrationId);
            return Ok(ApiResponse<RegistrationDto>.Ok(resultDto!, "Registration successful."));
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency or duplicate registration collision for user {UserId} and event {EventId}", userId, dto.EventId);
            return BadRequest(ApiResponse<RegistrationDto>.Fail("You have already registered for this event, or capacity was reached."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing event registration.");
            return StatusCode(500, ApiResponse<RegistrationDto>.Fail("An unexpected error occurred while processing your registration."));
        }
    }

    [HttpGet("my")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<ApiResponse<List<RegistrationDto>>>> GetMyRegistrations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<List<RegistrationDto>>.Fail("Unauthorized."));
        }

        var registrations = await _registrationRepository.GetUserRegistrationsAsync(userId);
        return Ok(ApiResponse<List<RegistrationDto>>.Ok(registrations));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<ApiResponse<bool>>> CancelRegistration(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<bool>.Fail("Unauthorized."));
        }

        var registration = await _registrationRepository.GetByIdAsync(id);
        if (registration == null)
        {
            return NotFound(ApiResponse<bool>.Fail("Registration not found."));
        }

        // Business Rule 11: Students cannot access or cancel another student's registration
        if (registration.UserId != userId)
        {
            return Forbid();
        }

        // Business Rule 7: Cannot cancel past event registration
        if (registration.Event != null && DateTime.UtcNow.Date > registration.Event.EventDate.Date)
        {
            return BadRequest(ApiResponse<bool>.Fail("Cannot cancel registration for a past event."));
        }

        if (registration.Status == RegistrationStatus.Cancelled)
        {
            return BadRequest(ApiResponse<bool>.Fail("This registration is already cancelled."));
        }

        registration.Status = RegistrationStatus.Cancelled;
        await _registrationRepository.UpdateAsync(registration);

        _logger.LogInformation("Student {UserId} cancelled registration {RegistrationId}", userId, id);
        return Ok(ApiResponse<bool>.Ok(true, "Registration cancelled successfully."));
    }
}
