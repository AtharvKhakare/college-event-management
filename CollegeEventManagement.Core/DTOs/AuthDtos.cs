using System.ComponentModel.DataAnnotations;

namespace CollegeEventManagement.Core.DTOs;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Student ID is required.")]
    [StringLength(30, ErrorMessage = "Student ID cannot exceed 30 characters.")]
    public string StudentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Academic Year is required.")]
    [StringLength(30, ErrorMessage = "Academic Year cannot exceed 30 characters.")]
    public string AcademicYear { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? Department { get; set; }
    public string? AcademicYear { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? Department { get; set; }
    public string? AcademicYear { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UpdateProfileDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    public string? Department { get; set; }

    [StringLength(30, ErrorMessage = "Academic Year cannot exceed 30 characters.")]
    public string? AcademicYear { get; set; }

    [Phone(ErrorMessage = "Invalid phone number.")]
    public string? PhoneNumber { get; set; }
}
