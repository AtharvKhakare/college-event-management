using System.Security.Claims;
using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class StudentController : Controller
{
    private readonly IApiService _apiService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(IApiService apiService, ILogger<StudentController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var myRegsResult = await _apiService.GetMyRegistrationsAsync();
        var registrations = myRegsResult.Success && myRegsResult.Data != null 
            ? myRegsResult.Data 
            : new List<RegistrationDto>();

        var today = DateTime.UtcNow.Date;
        var activeRegs = registrations.Where(r => r.Status != RegistrationStatus.Cancelled).ToList();

        var dashboard = new StudentDashboardDto
        {
            StudentName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Student",
            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            StudentId = User.FindFirst("StudentId")?.Value,
            Department = User.FindFirst("Department")?.Value,
            TotalRegisteredEvents = activeRegs.Count,
            UpcomingEventsCount = activeRegs.Count(r => r.EventDate.Date >= today),
            PastEventsCount = activeRegs.Count(r => r.EventDate.Date < today),
            AttendedEventsCount = activeRegs.Count(r => r.AttendanceStatus == AttendanceStatuses.Present),
            UpcomingRegistrations = activeRegs.Where(r => r.EventDate.Date >= today).OrderBy(r => r.EventDate).ToList(),
            PastRegistrations = activeRegs.Where(r => r.EventDate.Date < today).OrderByDescending(r => r.EventDate).ToList(),
            RecentRegistrations = registrations.Take(5).ToList()
        };

        return View(dashboard);
    }

    [HttpGet]
    [Route("Student/MyRegistrations")]
    [Route("Student/Registrations")]
    public async Task<IActionResult> MyRegistrations()
    {
        var response = await _apiService.GetMyRegistrationsAsync();
        var registrations = response.Success && response.Data != null 
            ? response.Data.Where(r => r.Status != RegistrationStatus.Cancelled).ToList()
            : new List<RegistrationDto>();

        return View(registrations);
    }

    [HttpGet]
    public async Task<IActionResult> RegistrationHistory()
    {
        var response = await _apiService.GetMyRegistrationsAsync();
        var registrations = response.Success && response.Data != null 
            ? response.Data 
            : new List<RegistrationDto>();

        return View(registrations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRegistration(int id, string? returnAction = null)
    {
        var response = await _apiService.CancelRegistrationAsync(id);
        if (response.Success)
        {
            TempData["SuccessMessage"] = "Your registration has been cancelled successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = response.Message ?? "Failed to cancel registration.";
        }

        if (returnAction == "Dashboard")
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return RedirectToAction(nameof(MyRegistrations));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var response = await _apiService.GetProfileAsync();
        if (!response.Success || response.Data == null)
        {
            TempData["ErrorMessage"] = "Could not load student profile.";
            return RedirectToAction(nameof(Dashboard));
        }

        return View(response.Data);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var response = await _apiService.GetProfileAsync();
        if (!response.Success || response.Data == null)
        {
            TempData["ErrorMessage"] = "Could not load student profile.";
            return RedirectToAction(nameof(Dashboard));
        }

        var editDto = new UpdateProfileDto
        {
            FullName = response.Data.FullName,
            Department = response.Data.Department,
            AcademicYear = response.Data.AcademicYear
        };

        return View(editDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var response = await _apiService.UpdateProfileAsync(dto);
        if (response.Success)
        {
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        TempData["ErrorMessage"] = response.Message ?? "Failed to update profile.";
        return View(dto);
    }
}
