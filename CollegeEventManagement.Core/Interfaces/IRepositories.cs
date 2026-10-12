using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Core.Entities;

namespace CollegeEventManagement.Core.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id);
    Task<EventDto?> GetEventDtoByIdAsync(int id, string? currentUserId = null);
    Task<PagedResult<EventDto>> GetAllAsync(EventFilterDto filter, string? currentUserId = null);
    Task<Event> AddAsync(Event ev);
    Task UpdateAsync(Event ev);
    Task DeleteAsync(int id);
    Task<int> GetRegisteredCountAsync(int eventId);
    Task<bool> IsUserRegisteredAsync(int eventId, string userId);
}

public interface IRegistrationRepository
{
    Task<EventRegistration?> GetByIdAsync(int id);
    Task<RegistrationDto?> GetRegistrationDtoByIdAsync(int id);
    Task<List<RegistrationDto>> GetUserRegistrationsAsync(string userId);
    Task<PagedResult<RegistrationDto>> GetAllAsync(RegistrationFilterDto filter);
    Task<EventRegistration> AddAsync(EventRegistration registration);
    Task UpdateAsync(EventRegistration registration);
    Task DeleteAsync(int id);
    Task<EventRegistration?> GetDuplicateRegistrationAsync(int eventId, string userId);
    Task<List<RegistrationDto>> GetExportRegistrationsAsync(int? eventId, string? status, string? attendanceStatus);
}

public interface IDashboardRepository
{
    Task<AdminDashboardDto> GetAdminDashboardDataAsync();
    Task<StudentDashboardDto> GetStudentDashboardDataAsync(string userId);
    Task<PagedResult<StudentAccountDto>> GetStudentsListAsync(string? search, int pageNumber = 1, int pageSize = 10);
}
