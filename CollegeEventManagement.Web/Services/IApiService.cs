using CollegeEventManagement.Core.DTOs;

namespace CollegeEventManagement.Web.Services;

public interface IApiService
{
    // Auth
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto);
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto);
    Task<ApiResponse<UserProfileDto>> GetProfileAsync();
    Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(UpdateProfileDto dto);

    // Events
    Task<ApiResponse<PagedResult<EventDto>>> GetEventsAsync(EventFilterDto filter);
    Task<ApiResponse<EventDto>> GetEventByIdAsync(int id);
    Task<ApiResponse<EventDto>> CreateEventAsync(CreateEventDto dto);
    Task<ApiResponse<EventDto>> UpdateEventAsync(int id, UpdateEventDto dto);
    Task<ApiResponse<bool>> DeleteEventAsync(int id);
    Task<ApiResponse<string>> UploadImageAsync(IFormFile file);

    // Registrations
    Task<ApiResponse<RegistrationDto>> RegisterForEventAsync(int eventId);
    Task<ApiResponse<List<RegistrationDto>>> GetMyRegistrationsAsync();
    Task<ApiResponse<bool>> CancelRegistrationAsync(int registrationId);

    // Admin
    Task<ApiResponse<AdminDashboardDto>> GetAdminDashboardAsync();
    Task<ApiResponse<PagedResult<RegistrationDto>>> GetAdminRegistrationsAsync(RegistrationFilterDto filter);
    Task<ApiResponse<PagedResult<StudentAccountDto>>> GetAdminStudentsAsync(string? search, int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<RegistrationDto>> UpdateRegistrationStatusAsync(int registrationId, string status);
    Task<ApiResponse<RegistrationDto>> UpdateAttendanceAsync(int registrationId, string attendanceStatus);
    Task<(byte[] Bytes, string FileName, string ContentType)?> ExportRegistrationsCsvAsync(int? eventId, string? status, string? attendanceStatus);
}
