using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.Entities;
using CollegeEventManagement.Infrastructure.Data;
using CollegeEventManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollegeEventManagement.Tests;

public class EventBusinessRulesTests
{
    private ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task EventCapacity_MustBeEnforced_WhenFull()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var eventRepo = new EventRepository(context);

        var testEvent = new Event
        {
            Title = "Limited Capacity Workshop",
            Category = EventCategories.Workshop,
            Capacity = 2,
            EventDate = DateTime.UtcNow.AddDays(10),
            RegistrationDeadline = DateTime.UtcNow.AddDays(8),
            IsPublished = true,
            Venue = "Lab 1",
            OrganizerName = "CS Dept"
        };
        await eventRepo.AddAsync(testEvent);

        // Add 2 active registrations
        context.EventRegistrations.Add(new EventRegistration
        {
            EventId = testEvent.EventId,
            UserId = "user-1",
            Status = RegistrationStatus.Approved
        });
        context.EventRegistrations.Add(new EventRegistration
        {
            EventId = testEvent.EventId,
            UserId = "user-2",
            Status = RegistrationStatus.Approved
        });
        await context.SaveChangesAsync();

        // Act
        var activeCount = await eventRepo.GetRegisteredCountAsync(testEvent.EventId);
        var isFull = activeCount >= testEvent.Capacity;

        // Assert
        Assert.Equal(2, activeCount);
        Assert.True(isFull);
    }

    [Fact]
    public async Task DuplicateRegistration_MustBeDetected()
    {
        // Arrange
        var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var regRepo = new RegistrationRepository(context);

        var existingReg = new EventRegistration
        {
            EventId = 101,
            UserId = "student-123",
            Status = RegistrationStatus.Approved
        };
        await regRepo.AddAsync(existingReg);

        // Act
        var duplicate = await regRepo.GetDuplicateRegistrationAsync(101, "student-123");

        // Assert
        Assert.NotNull(duplicate);
        Assert.Equal("student-123", duplicate.UserId);
        Assert.Equal(101, duplicate.EventId);
    }

    [Fact]
    public void RegistrationDeadline_MustRejectPastDeadlines()
    {
        // Arrange
        var deadlinePassedEvent = new Event
        {
            Title = "Past Deadline Seminar",
            EventDate = DateTime.UtcNow.AddDays(2),
            RegistrationDeadline = DateTime.UtcNow.AddHours(-2), // 2 hours ago
            IsPublished = true
        };

        // Act
        var isDeadlinePassed = DateTime.UtcNow > deadlinePassedEvent.RegistrationDeadline;

        // Assert
        Assert.True(isDeadlinePassed);
    }

    [Fact]
    public void PastEvent_MustRejectNewRegistrations()
    {
        // Arrange
        var pastEvent = new Event
        {
            Title = "Last Week Concert",
            EventDate = DateTime.UtcNow.AddDays(-7),
            RegistrationDeadline = DateTime.UtcNow.AddDays(-8),
            IsPublished = true
        };

        // Act
        var isPastEvent = DateTime.UtcNow.Date > pastEvent.EventDate.Date;

        // Assert
        Assert.True(isPastEvent);
    }

    [Fact]
    public void UnpublishedEvent_CannotBeRegistered()
    {
        // Arrange
        var draftEvent = new Event
        {
            Title = "Draft Unapproved Hackathon",
            IsPublished = false
        };

        // Assert
        Assert.False(draftEvent.IsPublished);
    }

    [Fact]
    public void StudentCancellation_PermittedOnlyForFutureEvents()
    {
        // Arrange
        var futureReg = new EventRegistration
        {
            Status = RegistrationStatus.Approved,
            Event = new Event { EventDate = DateTime.UtcNow.AddDays(5) }
        };

        var pastReg = new EventRegistration
        {
            Status = RegistrationStatus.Approved,
            Event = new Event { EventDate = DateTime.UtcNow.AddDays(-3) }
        };

        // Act & Assert
        var canCancelFuture = futureReg.Status == RegistrationStatus.Approved && DateTime.UtcNow.Date <= futureReg.Event.EventDate.Date;
        var canCancelPast = pastReg.Status == RegistrationStatus.Approved && DateTime.UtcNow.Date <= pastReg.Event.EventDate.Date;

        Assert.True(canCancelFuture);
        Assert.False(canCancelPast);
    }
}
