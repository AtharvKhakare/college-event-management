using System.Security.Claims;
using CollegeEventManagement.API.Services;
using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Validation failed", errors));
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("A user with this email address already exists."));
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName.Trim(),
            StudentId = dto.StudentId.Trim(),
            Department = dto.Department.Trim(),
            AcademicYear = dto.AcademicYear.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errorList = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Registration failed", errorList));
        }

        // Always assign Student role to public registrations
        await _userManager.AddToRoleAsync(user, AppRoles.Student);

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiration) = await _tokenService.GenerateTokenAsync(user, roles);

        var responseDto = new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            StudentId = user.StudentId,
            Department = user.Department,
            AcademicYear = user.AcademicYear,
            Roles = roles.ToList()
        };

        _logger.LogInformation("New student registered: {Email}", user.Email);
        return Ok(ApiResponse<AuthResponseDto>.Ok(responseDto, "Registration successful! Welcome to College Events."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Please provide valid login credentials."));
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid email or password."));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid email or password."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiration) = await _tokenService.GenerateTokenAsync(user, roles);

        var responseDto = new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            StudentId = user.StudentId,
            Department = user.Department,
            AcademicYear = user.AcademicYear,
            Roles = roles.ToList()
        };

        _logger.LogInformation("User logged in: {Email} ({Roles})", user.Email, string.Join(",", roles));
        return Ok(ApiResponse<AuthResponseDto>.Ok(responseDto, "Login successful."));
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("Unauthorized."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserProfileDto>.Fail("User profile not found."));
        }

        var roles = await _userManager.GetRolesAsync(user);

        var profileDto = new UserProfileDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            StudentId = user.StudentId,
            Department = user.Department,
            AcademicYear = user.AcademicYear,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };

        return Ok(ApiResponse<UserProfileDto>.Ok(profileDto));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("Unauthorized."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserProfileDto>.Fail("User not found."));
        }

        user.FullName = dto.FullName.Trim();
        if (!string.IsNullOrEmpty(dto.Department)) user.Department = dto.Department.Trim();
        if (!string.IsNullOrEmpty(dto.AcademicYear)) user.AcademicYear = dto.AcademicYear.Trim();
        if (!string.IsNullOrEmpty(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<UserProfileDto>.Fail("Failed to update profile", errors));
        }

        var roles = await _userManager.GetRolesAsync(user);

        var updatedProfile = new UserProfileDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            StudentId = user.StudentId,
            Department = user.Department,
            AcademicYear = user.AcademicYear,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };

        return Ok(ApiResponse<UserProfileDto>.Ok(updatedProfile, "Profile updated successfully."));
    }
}
