using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Web.Controllers;

public class HomeController : Controller
{
    private readonly IApiService _apiService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IApiService apiService, ILogger<HomeController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Load top 6 upcoming events for the hero & featured showcase
        var filter = new EventFilterDto
        {
            PageNumber = 1,
            PageSize = 6,
            OnlyPublished = true
        };

        var eventsResult = await _apiService.GetEventsAsync(filter);
        var featuredEvents = eventsResult.Success && eventsResult.Data != null 
            ? eventsResult.Data.Items 
            : new List<EventDto>();

        return View(featuredEvents);
    }

    [Route("/Home/NotFound")]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = 404;
        return View("NotFound");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
