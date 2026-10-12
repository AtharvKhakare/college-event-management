using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CollegeEventManagement.Core.DTOs;

namespace CollegeEventManagement.Web.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private void AddAuthHeader()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var token = user?.FindFirst("JWToken")?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // 1. Auth Methods
    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto)
    {
        return await PostAsync<LoginRequestDto, AuthResponseDto>("/api/auth/login", dto, false);
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto)
    {
        return await PostAsync<RegisterRequestDto, AuthResponseDto>("/api/auth/register", dto, false);
    }

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync()
    {
        return await GetAsync<UserProfileDto>("/api/auth/profile", true);
    }

    public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(UpdateProfileDto dto)
    {
        return await PutAsync<UpdateProfileDto, UserProfileDto>("/api/auth/profile", dto, true);
    }

    // 2. Events Methods
    public async Task<ApiResponse<PagedResult<EventDto>>> GetEventsAsync(EventFilterDto filter)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Search)) query.Add($"search={Uri.EscapeDataString(filter.Search)}");
        if (!string.IsNullOrWhiteSpace(filter.Category)) query.Add($"category={Uri.EscapeDataString(filter.Category)}");
        if (filter.FromDate.HasValue) query.Add($"fromDate={filter.FromDate.Value:yyyy-MM-dd}");
        if (filter.ToDate.HasValue) query.Add($"toDate={filter.ToDate.Value:yyyy-MM-dd}");
        if (filter.OnlyPublished.HasValue) query.Add($"onlyPublished={filter.OnlyPublished.Value}");
        query.Add($"pageNumber={filter.PageNumber}");
        query.Add($"pageSize={filter.PageSize}");

        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return await GetAsync<PagedResult<EventDto>>($"/api/events{queryString}", true);
    }

    public async Task<ApiResponse<EventDto>> GetEventByIdAsync(int id)
    {
        return await GetAsync<EventDto>($"/api/events/{id}", true);
    }

    public async Task<ApiResponse<EventDto>> CreateEventAsync(CreateEventDto dto)
    {
        return await PostAsync<CreateEventDto, EventDto>("/api/events", dto, true);
    }

    public async Task<ApiResponse<EventDto>> UpdateEventAsync(int id, UpdateEventDto dto)
    {
        return await PutAsync<UpdateEventDto, EventDto>($"/api/events/{id}", dto, true);
    }

    public async Task<ApiResponse<bool>> DeleteEventAsync(int id)
    {
        AddAuthHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/events/{id}");
            return await HandleResponseAsync<bool>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API delete call failed.");
            return ApiResponse<bool>.Fail("Could not connect to the backend server.");
        }
    }

    public async Task<ApiResponse<string>> UploadImageAsync(IFormFile file)
    {
        AddAuthHeader();
        try
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            var response = await _httpClient.PostAsync("/api/events/upload-image", content);
            return await HandleResponseAsync<string>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API image upload failed.");
            return ApiResponse<string>.Fail("Image upload failed due to connection error.");
        }
    }

    // 3. Registration Methods
    public async Task<ApiResponse<RegistrationDto>> RegisterForEventAsync(int eventId)
    {
        var dto = new CreateRegistrationDto { EventId = eventId };
        return await PostAsync<CreateRegistrationDto, RegistrationDto>("/api/registrations", dto, true);
    }

    public async Task<ApiResponse<List<RegistrationDto>>> GetMyRegistrationsAsync()
    {
        return await GetAsync<List<RegistrationDto>>("/api/registrations/my", true);
    }

    public async Task<ApiResponse<bool>> CancelRegistrationAsync(int registrationId)
    {
        AddAuthHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/registrations/{registrationId}");
            return await HandleResponseAsync<bool>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API cancel registration call failed.");
            return ApiResponse<bool>.Fail("Could not connect to the backend server.");
        }
    }

    // 4. Admin Methods
    public async Task<ApiResponse<AdminDashboardDto>> GetAdminDashboardAsync()
    {
        return await GetAsync<AdminDashboardDto>("/api/admin/dashboard", true);
    }

    public async Task<ApiResponse<PagedResult<RegistrationDto>>> GetAdminRegistrationsAsync(RegistrationFilterDto filter)
    {
        var query = new List<string>();
        if (filter.EventId.HasValue) query.Add($"eventId={filter.EventId.Value}");
        if (!string.IsNullOrWhiteSpace(filter.Status)) query.Add($"status={Uri.EscapeDataString(filter.Status)}");
        if (!string.IsNullOrWhiteSpace(filter.AttendanceStatus)) query.Add($"attendanceStatus={Uri.EscapeDataString(filter.AttendanceStatus)}");
        if (!string.IsNullOrWhiteSpace(filter.Search)) query.Add($"search={Uri.EscapeDataString(filter.Search)}");
        query.Add($"pageNumber={filter.PageNumber}");
        query.Add($"pageSize={filter.PageSize}");

        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return await GetAsync<PagedResult<RegistrationDto>>($"/api/admin/registrations{queryString}", true);
    }

    public async Task<ApiResponse<PagedResult<StudentAccountDto>>> GetAdminStudentsAsync(string? search, int pageNumber = 1, int pageSize = 10)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        query.Add($"pageNumber={pageNumber}");
        query.Add($"pageSize={pageSize}");

        var queryString = "?" + string.Join("&", query);
        return await GetAsync<PagedResult<StudentAccountDto>>($"/api/admin/students{queryString}", true);
    }

    public async Task<ApiResponse<RegistrationDto>> UpdateRegistrationStatusAsync(int registrationId, string status)
    {
        var dto = new UpdateRegistrationStatusDto { Status = status };
        return await PutAsync<UpdateRegistrationStatusDto, RegistrationDto>($"/api/admin/registrations/{registrationId}/status", dto, true);
    }

    public async Task<ApiResponse<RegistrationDto>> UpdateAttendanceAsync(int registrationId, string attendanceStatus)
    {
        var dto = new UpdateAttendanceDto { AttendanceStatus = attendanceStatus };
        return await PutAsync<UpdateAttendanceDto, RegistrationDto>($"/api/admin/registrations/{registrationId}/attendance", dto, true);
    }

    public async Task<(byte[] Bytes, string FileName, string ContentType)?> ExportRegistrationsCsvAsync(int? eventId, string? status, string? attendanceStatus)
    {
        AddAuthHeader();
        try
        {
            var query = new List<string>();
            if (eventId.HasValue) query.Add($"eventId={eventId.Value}");
            if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrWhiteSpace(attendanceStatus)) query.Add($"attendanceStatus={Uri.EscapeDataString(attendanceStatus)}");

            var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";
            var response = await _httpClient.GetAsync($"/api/admin/registrations/export{queryString}");

            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = response.Content.Headers.ContentDisposition?.FileNameStar 
                               ?? response.Content.Headers.ContentDisposition?.FileName 
                               ?? "event-registrations.csv";
                fileName = fileName.Trim('\"');
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "text/csv";
                return (bytes, fileName, contentType);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API CSV export failed.");
            return null;
        }
    }

    // Helper HTTP dispatchers
    private async Task<ApiResponse<TResponse>> GetAsync<TResponse>(string endpoint, bool attachAuth)
    {
        if (attachAuth) AddAuthHeader();
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            return await HandleResponseAsync<TResponse>(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP GET {Endpoint} failed", endpoint);
            return ApiResponse<TResponse>.Fail("Could not communicate with the backend API. Please make sure the API service is running.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling GET {Endpoint}", endpoint);
            return ApiResponse<TResponse>.Fail("An unexpected error occurred while communicating with the backend.");
        }
    }

    private async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, bool attachAuth)
    {
        if (attachAuth) AddAuthHeader();
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);
            return await HandleResponseAsync<TResponse>(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP POST {Endpoint} failed", endpoint);
            return ApiResponse<TResponse>.Fail("Could not communicate with the backend API. Please make sure the API service is running.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling POST {Endpoint}", endpoint);
            return ApiResponse<TResponse>.Fail("An unexpected error occurred while communicating with the backend.");
        }
    }

    private async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, bool attachAuth)
    {
        if (attachAuth) AddAuthHeader();
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(endpoint, content);
            return await HandleResponseAsync<TResponse>(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP PUT {Endpoint} failed", endpoint);
            return ApiResponse<TResponse>.Fail("Could not communicate with the backend API. Please make sure the API service is running.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling PUT {Endpoint}", endpoint);
            return ApiResponse<TResponse>.Fail("An unexpected error occurred while communicating with the backend.");
        }
    }

    private async Task<ApiResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
        {
            return response.IsSuccessStatusCode
                ? ApiResponse<T>.Ok(default!, "Operation completed successfully.")
                : ApiResponse<T>.Fail($"Request failed with status code {(int)response.StatusCode}.");
        }

        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
            if (apiResponse != null)
            {
                return apiResponse;
            }
        }
        catch (JsonException)
        {
            // If response was not formatted as ApiResponse<T>
        }

        return response.IsSuccessStatusCode
            ? ApiResponse<T>.Ok(default!, "Success")
            : ApiResponse<T>.Fail($"Request failed with status code {(int)response.StatusCode}: {content}");
    }
}
