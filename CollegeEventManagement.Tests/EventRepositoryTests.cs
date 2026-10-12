using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.DTOs;
using CollegeEventManagement.Core.Entities;
using CollegeEventManagement.Infrastructure.Data;
using CollegeEventManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollegeEventManagement.Tests;

public class EventRepositoryTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddEvent_ShouldPersistEventInDatabase()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var repo = new EventRepository(context);

        var ev = new Event
        {
            Title = "AI Symposium 2026",
            Description = "Symposium on cutting-edge neural networks.",
            Category = EventCategories.Technical,
            EventDate = DateTime.UtcNow.AddDays(14),
            StartTime = new TimeSpan(9, 30, 0),
            EndTime = new TimeSpan(17, 0, 0),
            Venue = "Hall 4",
            Capacity = 150,
            RegistrationDeadline = DateTime.UtcNow.AddDays(12),
            OrganizerName = "AI Society",
            IsPublished = true
        };

        // Act
        var added = await repo.AddAsync(ev);
        var retrieved = await repo.GetByIdAsync(added.EventId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("AI Symposium 2026", retrieved.Title);
        Assert.Equal(150, retrieved.Capacity);
    }

    [Fact]
    public async Task FilterEvents_ByCategoryAndSearch_ReturnsMatchingResults()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var repo = new EventRepository(context);

        await repo.AddAsync(new Event
        {
            Title = "Inter-College Cricket Tournament",
            Category = EventCategories.Sports,
            Venue = "Sports Ground",
            OrganizerName = "Sports Cell",
            IsPublished = true,
            Capacity = 50,
            EventDate = DateTime.UtcNow.AddDays(5)
        });

        await repo.AddAsync(new Event
        {
            Title = "Full-Stack Web Dev BootCamp",
            Category = EventCategories.Technical,
            Venue = "CS Lab",
            OrganizerName = "CS Club",
            IsPublished = true,
            Capacity = 60,
            EventDate = DateTime.UtcNow.AddDays(8)
        });

        // Act - Filter by Sports
        var sportsResult = await repo.GetAllAsync(new EventFilterDto { Category = EventCategories.Sports });
        // Act - Search by BootCamp
        var searchResult = await repo.GetAllAsync(new EventFilterDto { Search = "BootCamp" });

        // Assert
        Assert.Single(sportsResult.Items);
        Assert.Equal("Inter-College Cricket Tournament", sportsResult.Items.First().Title);

        Assert.Single(searchResult.Items);
        Assert.Equal("Full-Stack Web Dev BootCamp", searchResult.Items.First().Title);
    }

    [Fact]
    public async Task UpdateEvent_ShouldUpdateProperties()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var repo = new EventRepository(context);

        var ev = await repo.AddAsync(new Event
        {
            Title = "Original Title",
            Category = EventCategories.Seminar,
            Venue = "Room 101",
            OrganizerName = "Staff",
            Capacity = 40,
            EventDate = DateTime.UtcNow.AddDays(10)
        });

        // Act
        ev.Title = "Updated Title";
        ev.Capacity = 80;
        await repo.UpdateAsync(ev);

        var updated = await repo.GetByIdAsync(ev.EventId);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal(80, updated.Capacity);
    }

    [Fact]
    public async Task DeleteEvent_ShouldRemoveFromDatabase()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var repo = new EventRepository(context);

        var ev = await repo.AddAsync(new Event
        {
            Title = "Event to Delete",
            Category = EventCategories.Cultural,
            Venue = "Auditorium",
            OrganizerName = "Music Club",
            Capacity = 100,
            EventDate = DateTime.UtcNow.AddDays(12)
        });

        // Act
        await repo.DeleteAsync(ev.EventId);
        var retrieved = await repo.GetByIdAsync(ev.EventId);

        // Assert
        Assert.Null(retrieved);
    }
}
