using System.Security.Claims;
using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Web.Controllers;

public class AccountController : Controller
{
    private readonly IApiService _apiService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IApiService apiService, ILogger<AccountController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDefaultDashboard();
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestDto dto, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var response = await _apiService.LoginAsync(dto);
        if (!response.Success || response.Data == null)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Invalid email or password.");
            return View(dto);
        }

        // Sign in user with cookie claims including JWT token
        await SignInUserWithClaimsAsync(response.Data);

        TempData["SuccessMessage"] = $"Welcome back, {response.Data.FullName}!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToDefaultDashboard(response.Data.Roles);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDefaultDashboard();
        }

        return View(new RegisterRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var response = await _apiService.RegisterAsync(dto);
        if (!response.Success || response.Data == null)
        {
            if (response.Errors != null && response.Errors.Any())
            {
                foreach (var err in response.Errors)
                {
                    ModelState.AddModelError(string.Empty, err);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, response.Message ?? "Registration failed.");
            }
            return View(dto);
        }

        // Sign in newly registered student
        await SignInUserWithClaimsAsync(response.Data);

        TempData["SuccessMessage"] = "Your student account was created successfully! Welcome to CampusEvents.";
        return RedirectToAction("Dashboard", "Student");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInUserWithClaimsAsync(AuthResponseDto auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.UserId),
            new(ClaimTypes.Name, auth.FullName),
            new(ClaimTypes.Email, auth.Email),
            new("JWToken", auth.Token) // Used by ApiService to authenticate HTTP requests to Web API
        };

        if (!string.IsNullOrEmpty(auth.StudentId))
        {
            claims.Add(new Claim("StudentId", auth.StudentId));
        }

        if (!string.IsNullOrEmpty(auth.Department))
        {
            claims.Add(new Claim("Department", auth.Department));
        }

        foreach (var role in auth.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = auth.Expiration
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    private IActionResult RedirectToDefaultDashboard(IList<string>? roles = null)
    {
        if (roles == null)
        {
            if (User.IsInRole(AppRoles.Admin)) return RedirectToAction("Index", "Admin");
            return RedirectToAction("Dashboard", "Student");
        }

        if (roles.Contains(AppRoles.Admin))
        {
            return RedirectToAction("Index", "Admin");
        }

        return RedirectToAction("Dashboard", "Student");
    }
}
