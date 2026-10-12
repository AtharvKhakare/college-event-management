using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Web.Controllers;

public class EventsController : Controller
{
    private readonly IApiService _apiService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IApiService apiService, ILogger<EventsController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EventFilterDto filter)
    {
        filter.PageSize = 9; // 9 cards per page
        filter.OnlyPublished = true;

        var response = await _apiService.GetEventsAsync(filter);
        var result = response.Success && response.Data != null 
            ? response.Data 
            : new PagedResult<EventDto>();

        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var response = await _apiService.GetEventByIdAsync(id);
        if (!response.Success || response.Data == null)
        {
            TempData["ErrorMessage"] = response.Message ?? "Event not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(response.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            TempData["ErrorMessage"] = "Please log in with your student account to register for events.";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "Events", new { id }) });
        }

        if (User.IsInRole(AppRoles.Admin))
        {
            TempData["ErrorMessage"] = "Administrators manage events. Please log in with a student account to register as an attendee.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var response = await _apiService.RegisterForEventAsync(id);
        if (response.Success)
        {
            TempData["SuccessMessage"] = response.Message ?? "Registration successful! You're booked for this event.";
        }
        else
        {
            TempData["ErrorMessage"] = response.Message ?? "Registration failed.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
