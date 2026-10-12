using System.Text;
using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.Entities;
using CollegeEventManagement.Infrastructure.Data;
using CollegeEventManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollegeEventManagement.Tests;

public class RegistrationAndReportTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetUserRegistrations_MustOnlyReturnCurrentStudentsRegistrations()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var repo = new RegistrationRepository(context);

        var userA = new ApplicationUser { Id = "student-A", FullName = "Student A", Email = "a@test.com" };
        var userB = new ApplicationUser { Id = "student-B", FullName = "Student B", Email = "b@test.com" };
        context.Users.AddRange(userA, userB);

        var ev = new Event
        {
            Title = "Tech Fest",
            Category = EventCategories.Technical,
            Venue = "Hall A",
            OrganizerName = "Club"
        };
        context.Events.Add(ev);
        await context.SaveChangesAsync();

        var regStudent1 = new EventRegistration
        {
            EventId = ev.EventId,
            UserId = userA.Id,
            Status = RegistrationStatus.Approved,
            RegistrationDate = DateTime.UtcNow,
            User = userA,
            Event = ev
        };
        var regStudent2 = new EventRegistration
        {
            EventId = ev.EventId,
            UserId = userB.Id,
            Status = RegistrationStatus.Approved,
            RegistrationDate = DateTime.UtcNow,
            User = userB,
            Event = ev
        };

        context.EventRegistrations.AddRange(regStudent1, regStudent2);
        await context.SaveChangesAsync();

        // Act
        var studentARegistrations = await repo.GetUserRegistrationsAsync("student-A");

        // Assert - Student A should only see their own registration
        Assert.Single(studentARegistrations);
        Assert.Equal("student-A", studentARegistrations.First().UserId);
    }

    [Fact]
    public async Task ExportRegistrations_ShouldGenerateAccurateCsvOutput()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var repo = new RegistrationRepository(context);

        var user = new ApplicationUser
        {
            Id = "user-100",
            FullName = "Aarav Sharma",
            Email = "aarav@college.edu",
            StudentId = "CS2026",
            Department = "Computer Science",
            AcademicYear = "3rd Year"
        };
        context.Users.Add(user);

        var ev = new Event
        {
            EventId = 200,
            Title = "Annual Hackathon",
            Category = EventCategories.Technical,
            EventDate = new DateTime(2026, 11, 20),
            Venue = "Innovation Center",
            OrganizerName = "Coding Club"
        };
        context.Events.Add(ev);

        context.EventRegistrations.Add(new EventRegistration
        {
            RegistrationId = 55,
            UserId = user.Id,
            EventId = ev.EventId,
            RegistrationDate = new DateTime(2026, 10, 15, 10, 30, 0),
            Status = RegistrationStatus.Approved,
            AttendanceStatus = AttendanceStatuses.Present,
            User = user,
            Event = ev
        });
        await context.SaveChangesAsync();

        // Act
        var exportData = await repo.GetExportRegistrationsAsync(200, RegistrationStatus.Approved, null);

        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("RegistrationId,StudentName,StudentEmail,StudentId,Department,AcademicYear,EventTitle,Category,EventDate,Venue,RegistrationDate,Status,AttendanceStatus");
        foreach (var r in exportData)
        {
            csvBuilder.AppendLine($"\"{r.RegistrationId}\",\"{r.StudentName}\",\"{r.StudentEmail}\",\"{r.StudentId}\",\"{r.Department}\",\"{r.AcademicYear}\",\"{r.EventTitle}\",\"{r.EventCategory}\",\"{r.EventDate:yyyy-MM-dd}\",\"{r.Venue}\",\"{r.RegistrationDate:yyyy-MM-dd HH:mm:ss}\",\"{r.Status}\",\"{r.AttendanceStatus}\"");
        }
        var csvContent = csvBuilder.ToString();

        // Assert
        Assert.Single(exportData);
        Assert.Contains("Aarav Sharma", csvContent);
        Assert.Contains("aarav@college.edu", csvContent);
        Assert.Contains("Annual Hackathon", csvContent);
        Assert.Contains("Present", csvContent);
    }
}
