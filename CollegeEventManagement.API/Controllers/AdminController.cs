using System.Text;
using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminController : ControllerBase
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IDashboardRepository dashboardRepository,
        IRegistrationRepository registrationRepository,
        ILogger<AdminController> logger)
    {
        _dashboardRepository = dashboardRepository;
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetDashboard()
    {
        var data = await _dashboardRepository.GetAdminDashboardDataAsync();
        return Ok(ApiResponse<AdminDashboardDto>.Ok(data));
    }

    [HttpGet("registrations")]
    public async Task<ActionResult<ApiResponse<PagedResult<RegistrationDto>>>> GetRegistrations([FromQuery] RegistrationFilterDto filter)
    {
        var result = await _registrationRepository.GetAllAsync(filter);
        return Ok(ApiResponse<PagedResult<RegistrationDto>>.Ok(result));
    }

    [HttpGet("students")]
    public async Task<ActionResult<ApiResponse<PagedResult<StudentAccountDto>>>> GetStudents([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _dashboardRepository.GetStudentsListAsync(search, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<StudentAccountDto>>.Ok(result));
    }

    [HttpPut("registrations/{id:int}/status")]
    public async Task<ActionResult<ApiResponse<RegistrationDto>>> UpdateStatus(int id, [FromBody] UpdateRegistrationStatusDto dto)
    {
        if (!RegistrationStatus.All.Contains(dto.Status))
        {
            return BadRequest(ApiResponse<RegistrationDto>.Fail($"Invalid registration status '{dto.Status}'."));
        }

        var reg = await _registrationRepository.GetByIdAsync(id);
        if (reg == null)
        {
            return NotFound(ApiResponse<RegistrationDto>.Fail("Registration not found."));
        }

        reg.Status = dto.Status;
        await _registrationRepository.UpdateAsync(reg);

        var updatedDto = await _registrationRepository.GetRegistrationDtoByIdAsync(id);
        _logger.LogInformation("Admin updated registration {Id} status to {Status}", id, dto.Status);

        return Ok(ApiResponse<RegistrationDto>.Ok(updatedDto!, $"Registration status updated to {dto.Status}."));
    }

    [HttpPut("registrations/{id:int}/attendance")]
    public async Task<ActionResult<ApiResponse<RegistrationDto>>> UpdateAttendance(int id, [FromBody] UpdateAttendanceDto dto)
    {
        if (!AttendanceStatuses.All.Contains(dto.AttendanceStatus))
        {
            return BadRequest(ApiResponse<RegistrationDto>.Fail($"Invalid attendance status '{dto.AttendanceStatus}'."));
        }

        var reg = await _registrationRepository.GetByIdAsync(id);
        if (reg == null)
        {
            return NotFound(ApiResponse<RegistrationDto>.Fail("Registration not found."));
        }

        reg.AttendanceStatus = dto.AttendanceStatus;
        await _registrationRepository.UpdateAsync(reg);

        var updatedDto = await _registrationRepository.GetRegistrationDtoByIdAsync(id);
        _logger.LogInformation("Admin marked attendance for registration {Id} as {Attendance}", id, dto.AttendanceStatus);

        return Ok(ApiResponse<RegistrationDto>.Ok(updatedDto!, $"Attendance marked as {dto.AttendanceStatus}."));
    }

    [HttpGet("registrations/export")]
    public async Task<IActionResult> ExportRegistrations([FromQuery] int? eventId, [FromQuery] string? status, [FromQuery] string? attendanceStatus)
    {
        var registrations = await _registrationRepository.GetExportRegistrationsAsync(eventId, status, attendanceStatus);

        var builder = new StringBuilder();
        // CSV Header
        builder.AppendLine("RegistrationId,StudentName,StudentEmail,StudentId,Department,AcademicYear,EventTitle,Category,EventDate,Venue,RegistrationDate,Status,AttendanceStatus");

        foreach (var r in registrations)
        {
            var line = string.Format(
                "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\",\"{12}\"",
                r.RegistrationId,
                EscapeCsv(r.StudentName),
                EscapeCsv(r.StudentEmail),
                EscapeCsv(r.StudentId ?? ""),
                EscapeCsv(r.Department ?? ""),
                EscapeCsv(r.AcademicYear ?? ""),
                EscapeCsv(r.EventTitle),
                EscapeCsv(r.EventCategory),
                r.EventDate.ToString("yyyy-MM-dd"),
                EscapeCsv(r.Venue),
                r.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss"),
                r.Status,
                r.AttendanceStatus
            );
            builder.AppendLine(line);
        }

        var csvBytes = Encoding.UTF8.GetBytes(builder.ToString());
        var fileName = $"event-registrations-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        return File(csvBytes, "text/csv", fileName);
    }

    private static string EscapeCsv(string value)
    {
        return value.Replace("\"", "\"\"");
    }
}
