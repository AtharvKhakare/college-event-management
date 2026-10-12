using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly IApiService _apiService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IApiService apiService, ILogger<AdminController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await _apiService.GetAdminDashboardAsync();
        var dashboard = response.Success && response.Data != null 
            ? response.Data 
            : new AdminDashboardDto();

        return View(dashboard);
    }

    [HttpGet]
    public async Task<IActionResult> Events([FromQuery] EventFilterDto filter)
    {
        filter.PageSize = 15;
        filter.OnlyPublished = null; // Admin can view both published and draft events

        var response = await _apiService.GetEventsAsync(filter);
        var result = response.Success && response.Data != null
            ? response.Data
            : new PagedResult<EventDto>();

        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        var model = new CreateEventDto
        {
            EventDate = DateTime.UtcNow.AddDays(7).Date,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(16, 0, 0),
            RegistrationDeadline = DateTime.UtcNow.AddDays(5).Date.AddHours(23),
            Capacity = 100,
            IsPublished = true
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(CreateEventDto dto, IFormFile? posterFile)
    {
        if (posterFile != null && posterFile.Length > 0)
        {
            var uploadRes = await _apiService.UploadImageAsync(posterFile);
            if (uploadRes.Success && !string.IsNullOrEmpty(uploadRes.Data))
            {
                dto.ImageUrl = uploadRes.Data;
            }
        }

        if (string.IsNullOrWhiteSpace(dto.ImageUrl))
        {
            dto.ImageUrl = "/images/events/default-event.jpg";
        }

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var response = await _apiService.CreateEventAsync(dto);
        if (response.Success)
        {
            TempData["SuccessMessage"] = "Event created successfully!";
            return RedirectToAction(nameof(Events));
        }

        TempData["ErrorMessage"] = response.Message ?? "Failed to create event.";
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var response = await _apiService.GetEventByIdAsync(id);
        if (!response.Success || response.Data == null)
        {
            TempData["ErrorMessage"] = "Event not found.";
            return RedirectToAction(nameof(Events));
        }

        var ev = response.Data;
        var editDto = new UpdateEventDto
        {
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
            IsPublished = ev.IsPublished
        };

        ViewBag.EventId = id;
        return View(editDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, UpdateEventDto dto, IFormFile? posterFile)
    {
        ViewBag.EventId = id;

        if (posterFile != null && posterFile.Length > 0)
        {
            var uploadRes = await _apiService.UploadImageAsync(posterFile);
            if (uploadRes.Success && !string.IsNullOrEmpty(uploadRes.Data))
            {
                dto.ImageUrl = uploadRes.Data;
            }
        }

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var response = await _apiService.UpdateEventAsync(id, dto);
        if (response.Success)
        {
            TempData["SuccessMessage"] = "Event updated successfully!";
            return RedirectToAction(nameof(Events));
        }

        TempData["ErrorMessage"] = response.Message ?? "Failed to update event.";
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var response = await _apiService.DeleteEventAsync(id);
        if (response.Success)
        {
            TempData["SuccessMessage"] = "Event deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = response.Message ?? "Failed to delete event.";
        }

        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EventDetails(int id)
    {
        var evResponse = await _apiService.GetEventByIdAsync(id);
        if (!evResponse.Success || evResponse.Data == null)
        {
            TempData["ErrorMessage"] = "Event not found.";
            return RedirectToAction(nameof(Events));
        }

        var regFilter = new RegistrationFilterDto
        {
            EventId = id,
            PageSize = 100
        };
        var regResponse = await _apiService.GetAdminRegistrationsAsync(regFilter);

        ViewBag.Registrations = regResponse.Success && regResponse.Data != null 
            ? regResponse.Data.Items 
            : new List<RegistrationDto>();

        return View(evResponse.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Registrations([FromQuery] RegistrationFilterDto filter)
    {
        filter.PageSize = 20;
        var response = await _apiService.GetAdminRegistrationsAsync(filter);
        var result = response.Success && response.Data != null
            ? response.Data
            : new PagedResult<RegistrationDto>();

        // Load events for the event dropdown filter
        var eventsResponse = await _apiService.GetEventsAsync(new EventFilterDto { PageSize = 100 });
        ViewBag.Events = eventsResponse.Success && eventsResponse.Data != null ? eventsResponse.Data.Items : new List<EventDto>();
        ViewBag.Filter = filter;

        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRegistrationStatus(int id, string status, string? returnUrl = null)
    {
        var response = await _apiService.UpdateRegistrationStatusAsync(id, status);
        if (response.Success)
        {
            TempData["SuccessMessage"] = $"Registration status updated to {status}.";
        }
        else
        {
            TempData["ErrorMessage"] = response.Message ?? "Failed to update status.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Registrations));
    }

    [HttpGet]
    public async Task<IActionResult> Students([FromQuery] string? search, [FromQuery] int pageNumber = 1)
    {
        var response = await _apiService.GetAdminStudentsAsync(search, pageNumber, 15);
        var result = response.Success && response.Data != null
            ? response.Data
            : new PagedResult<StudentAccountDto>();

        ViewBag.Search = search;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Attendance([FromQuery] int? eventId, [FromQuery] string? search)
    {
        var filter = new RegistrationFilterDto
        {
            EventId = eventId,
            Search = search,
            Status = RegistrationStatus.Approved, // Only show approved registrations for attendance
            PageSize = 50
        };

        var response = await _apiService.GetAdminRegistrationsAsync(filter);
        var result = response.Success && response.Data != null
            ? response.Data
            : new PagedResult<RegistrationDto>();

        var eventsResponse = await _apiService.GetEventsAsync(new EventFilterDto { PageSize = 100 });
        ViewBag.Events = eventsResponse.Success && eventsResponse.Data != null ? eventsResponse.Data.Items : new List<EventDto>();
        ViewBag.SelectedEventId = eventId;
        ViewBag.Search = search;

        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttendance(int id, string attendanceStatus, int? eventId = null, string? search = null)
    {
        var response = await _apiService.UpdateAttendanceAsync(id, attendanceStatus);
        if (response.Success)
        {
            TempData["SuccessMessage"] = $"Attendance updated to {attendanceStatus}.";
        }
        else
        {
            TempData["ErrorMessage"] = response.Message ?? "Failed to update attendance.";
        }

        return RedirectToAction(nameof(Attendance), new { eventId, search });
    }

    [HttpGet]
    public async Task<IActionResult> Reports([FromQuery] int? eventId, [FromQuery] string? status, [FromQuery] string? attendanceStatus)
    {
        var eventsResponse = await _apiService.GetEventsAsync(new EventFilterDto { PageSize = 100 });
        ViewBag.Events = eventsResponse.Success && eventsResponse.Data != null ? eventsResponse.Data.Items : new List<EventDto>();
        ViewBag.EventId = eventId;
        ViewBag.Status = status;
        ViewBag.AttendanceStatus = attendanceStatus;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv([FromQuery] int? eventId, [FromQuery] string? status, [FromQuery] string? attendanceStatus)
    {
        var exportResult = await _apiService.ExportRegistrationsCsvAsync(eventId, status, attendanceStatus);
        if (exportResult == null)
        {
            TempData["ErrorMessage"] = "Failed to generate CSV export.";
            return RedirectToAction(nameof(Reports));
        }

        return File(exportResult.Value.Bytes, exportResult.Value.ContentType, exportResult.Value.FileName);
    }
}
