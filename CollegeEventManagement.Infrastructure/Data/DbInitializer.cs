using CollegeEventManagement.Core.Constants;
using CollegeEventManagement.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CollegeEventManagement.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // 1. Ensure database is created and apply pending migrations
        try
        {
            if (context.Database.IsSqlServer())
            {
                logger.LogInformation("Applying SQL Server migrations...");
                await context.Database.MigrateAsync();
            }
            else
            {
                logger.LogInformation("Database provider is '{Provider}'. Ensuring database schema exists...", context.Database.ProviderName);
                await context.Database.EnsureCreatedAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }

        // 2. Seed Roles
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogInformation("Creating role: {Role}", roleName);
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 3. Seed Default Administrator Account
        var adminEmail = configuration["AdminAccount:Email"] ?? "admin@collegeevents.edu";
        var adminPassword = configuration["AdminAccount:Password"] ?? "Admin@Secure123!";
        var adminName = configuration["AdminAccount:FullName"] ?? "System Administrator";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            logger.LogInformation("Seeding administrator account: {Email}", adminEmail);
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = adminName,
                StudentId = "ADMIN-001",
                Department = "Event Administration",
                AcademicYear = "Staff",
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                logger.LogInformation("Administrator account created successfully.");
            }
            else
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                logger.LogWarning("Failed to create admin user: {Errors}", errors);
            }
        }

        // 4. Seed Default Student Account for immediate testing
        var sampleStudentEmail = "student@collegeevents.edu";
        var sampleStudentPassword = "Student@Secure123!";
        var sampleStudent = await userManager.FindByEmailAsync(sampleStudentEmail);
        if (sampleStudent == null)
        {
            logger.LogInformation("Seeding sample student account: {Email}", sampleStudentEmail);
            sampleStudent = new ApplicationUser
            {
                UserName = sampleStudentEmail,
                Email = sampleStudentEmail,
                EmailConfirmed = true,
                FullName = "Atharv Sharma",
                StudentId = "CS2024001",
                Department = "Computer Science & Engineering",
                AcademicYear = "3rd Year",
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            };

            var studentResult = await userManager.CreateAsync(sampleStudent, sampleStudentPassword);
            if (studentResult.Succeeded)
            {
                await userManager.AddToRoleAsync(sampleStudent, AppRoles.Student);
            }
        }

        // Seed second student for attendance stats
        var student2Email = "priya.patel@collegeevents.edu";
        var student2 = await userManager.FindByEmailAsync(student2Email);
        if (student2 == null)
        {
            student2 = new ApplicationUser
            {
                UserName = student2Email,
                Email = student2Email,
                EmailConfirmed = true,
                FullName = "Priya Patel",
                StudentId = "IT2024045",
                Department = "Information Technology",
                AcademicYear = "2nd Year",
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };
            var s2Result = await userManager.CreateAsync(student2, "Student@Secure123!");
            if (s2Result.Succeeded)
            {
                await userManager.AddToRoleAsync(student2, AppRoles.Student);
            }
        }

        // 5. Seed Events (at least 8 events across Technical, Cultural, Sports, Workshop, Seminar)
        if (!await context.Events.AnyAsync())
        {
            logger.LogInformation("Seeding sample college events...");

            var today = DateTime.UtcNow.Date;

            var sampleEvents = new List<Event>
            {
                new()
                {
                    Title = "CodeStorm 2026: 24-Hour National Hackathon",
                    Description = "Bring your laptops, assemble your team of 4, and solve real-world industry challenges spanning AI, Cloud, and Web3 in an exhilarating 24-hour non-stop code sprint with exciting cash prizes and mentoring from top tech leaders.",
                    Category = EventCategories.Technical,
                    ImageUrl = "/images/events/hackathon.jpg",
                    EventDate = today.AddDays(15),
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    Venue = "Innovation & Incubation Hub, Block B",
                    Capacity = 120,
                    RegistrationDeadline = today.AddDays(12).AddHours(23),
                    OrganizerName = "Department of Computer Science & Coding Club",
                    CreatedAt = today.AddDays(-10),
                    IsPublished = true
                },
                new()
                {
                    Title = "RoboWar & Drone Racing Championship",
                    Description = "Witness the clash of titanium and silicon! Compete with your custom-built battle bots in our reinforced arena or maneuver acrobatic FPV drones across our multi-obstacle racing course.",
                    Category = EventCategories.Technical,
                    ImageUrl = "/images/events/robotics.jpg",
                    EventDate = today.AddDays(22),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(16, 30, 0),
                    Venue = "Central Auditorium Arena",
                    Capacity = 200,
                    RegistrationDeadline = today.AddDays(19).AddHours(23),
                    OrganizerName = "Robotics & Automation Society",
                    CreatedAt = today.AddDays(-8),
                    IsPublished = true
                },
                new()
                {
                    Title = "Aura 2026: Annual Inter-College Cultural Fest",
                    Description = "A magnificent celebration of music, dance, theatrical arts, and fashion. Featuring live band performances, celebrity judges, street dance battles, and classical solo competitions.",
                    Category = EventCategories.Cultural,
                    ImageUrl = "/images/events/cultural.jpg",
                    EventDate = today.AddDays(30),
                    StartTime = new TimeSpan(16, 0, 0),
                    EndTime = new TimeSpan(22, 0, 0),
                    Venue = "Open Air Amphitheatre",
                    Capacity = 500,
                    RegistrationDeadline = today.AddDays(25).AddHours(23),
                    OrganizerName = "Student Cultural Committee",
                    CreatedAt = today.AddDays(-15),
                    IsPublished = true
                },
                new()
                {
                    Title = "Rhythm & Verse: Slam Poetry and Acoustic Night",
                    Description = "An evening of expressive spoken word poetry, indie acoustic performances, and heartfelt original songwriting under the stars. Free refreshments provided for registered attendees.",
                    Category = EventCategories.Cultural,
                    ImageUrl = "/images/events/music.jpg",
                    EventDate = today.AddDays(8),
                    StartTime = new TimeSpan(17, 30, 0),
                    EndTime = new TimeSpan(20, 30, 0),
                    Venue = "Student Activity Center Courtyard",
                    Capacity = 80,
                    RegistrationDeadline = today.AddDays(6).AddHours(23),
                    OrganizerName = "Literary & Performing Arts Guild",
                    CreatedAt = today.AddDays(-5),
                    IsPublished = true
                },
                new()
                {
                    Title = "Inter-Department Football Tournament 2026",
                    Description = "The ultimate showdown of athletic excellence! 7-a-side departmental knockout matches over three exhilarating days. Medals, trophies, and bragging rights await the champions.",
                    Category = EventCategories.Sports,
                    ImageUrl = "/images/events/football.jpg",
                    EventDate = today.AddDays(18),
                    StartTime = new TimeSpan(7, 30, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    Venue = "Main Sports Ground & Stadium",
                    Capacity = 160,
                    RegistrationDeadline = today.AddDays(14).AddHours(23),
                    OrganizerName = "Department of Physical Education",
                    CreatedAt = today.AddDays(-12),
                    IsPublished = true
                },
                new()
                {
                    Title = "Annual Badminton & Table Tennis Open",
                    Description = "Singles and doubles tournaments for men and women across all academic years. Professional referees, standard Yonex gear, and live scoring displays.",
                    Category = EventCategories.Sports,
                    ImageUrl = "/images/events/badminton.jpg",
                    EventDate = today.AddDays(11),
                    StartTime = new TimeSpan(8, 30, 0),
                    EndTime = new TimeSpan(15, 0, 0),
                    Venue = "Indoor Sports Complex",
                    Capacity = 64,
                    RegistrationDeadline = today.AddDays(9).AddHours(23),
                    OrganizerName = "Sports Club",
                    CreatedAt = today.AddDays(-7),
                    IsPublished = true
                },
                new()
                {
                    Title = "Hands-on Workshop: Building Generative AI & LLM Apps",
                    Description = "An intensive practical workshop covering LangChain, Semantic Kernel, Vector Databases, and Retrieval-Augmented Generation (RAG). Participants will build and deploy a functional AI agent by end of day.",
                    Category = EventCategories.Workshop,
                    ImageUrl = "/images/events/ai-workshop.jpg",
                    EventDate = today.AddDays(5),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(15, 30, 0),
                    Venue = "Advanced Computing Lab 4, Tech Park",
                    Capacity = 60,
                    RegistrationDeadline = today.AddDays(3).AddHours(23),
                    OrganizerName = "AI & Data Science Student Chapter",
                    CreatedAt = today.AddDays(-4),
                    IsPublished = true
                },
                new()
                {
                    Title = "Cloud Architecture & DevOps Masterclass",
                    Description = "Deep dive into containerization with Docker, Kubernetes cluster orchestration, GitHub Actions CI/CD pipelines, and infrastructure as code with Terraform. Hands-on cloud credits provided.",
                    Category = EventCategories.Workshop,
                    ImageUrl = "/images/events/cloud.jpg",
                    EventDate = today.AddDays(20),
                    StartTime = new TimeSpan(9, 30, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    Venue = "Seminar Hall 2, Science Complex",
                    Capacity = 75,
                    RegistrationDeadline = today.AddDays(17).AddHours(23),
                    OrganizerName = "Google Developer Student Club (GDSC)",
                    CreatedAt = today.AddDays(-6),
                    IsPublished = true
                },
                new()
                {
                    Title = "Global Seminar: Future of Tech Careers & Silicon Valley Trends",
                    Description = "Keynote address by distinguished college alumni working at leading tech enterprises. Topics include tech interview strategy, emerging tech horizons, and global scholarship pathways.",
                    Category = EventCategories.Seminar,
                    ImageUrl = "/images/events/seminar.jpg",
                    EventDate = today.AddDays(25),
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    Venue = "Main University Convention Center",
                    Capacity = 300,
                    RegistrationDeadline = today.AddDays(23).AddHours(23),
                    OrganizerName = "Career Guidance & Placement Cell",
                    CreatedAt = today.AddDays(-9),
                    IsPublished = true
                },
                new()
                {
                    Title = "Past Showcase: Tech-Ex 2026 Project Exhibition",
                    Description = "Completed annual project exhibition displaying capstone projects from final year engineering students with industry judge evaluations.",
                    Category = EventCategories.Technical,
                    ImageUrl = "/images/events/exhibition.jpg",
                    EventDate = today.AddDays(-10),
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    Venue = "Exhibition Hall A",
                    Capacity = 100,
                    RegistrationDeadline = today.AddDays(-12),
                    OrganizerName = "Academic Dean Office",
                    CreatedAt = today.AddDays(-30),
                    IsPublished = true
                }
            };

            await context.Events.AddRangeAsync(sampleEvents);
            await context.SaveChangesAsync();

            // 6. Seed initial registrations for rich dashboard data
            if (sampleStudent != null && sampleEvents.Count >= 4)
            {
                var reg1 = new EventRegistration
                {
                    UserId = sampleStudent.Id,
                    EventId = sampleEvents[0].EventId, // Hackathon
                    RegistrationDate = today.AddDays(-5),
                    Status = RegistrationStatus.Approved,
                    AttendanceStatus = AttendanceStatuses.Unmarked
                };

                var reg2 = new EventRegistration
                {
                    UserId = sampleStudent.Id,
                    EventId = sampleEvents[6].EventId, // AI Workshop
                    RegistrationDate = today.AddDays(-3),
                    Status = RegistrationStatus.Approved,
                    AttendanceStatus = AttendanceStatuses.Unmarked
                };

                var reg3 = new EventRegistration
                {
                    UserId = sampleStudent.Id,
                    EventId = sampleEvents[9].EventId, // Past Event
                    RegistrationDate = today.AddDays(-20),
                    Status = RegistrationStatus.Approved,
                    AttendanceStatus = AttendanceStatuses.Present
                };

                context.EventRegistrations.AddRange(reg1, reg2, reg3);
            }

            if (student2 != null && sampleEvents.Count >= 4)
            {
                var reg4 = new EventRegistration
                {
                    UserId = student2.Id,
                    EventId = sampleEvents[0].EventId, // Hackathon
                    RegistrationDate = today.AddDays(-4),
                    Status = RegistrationStatus.Approved,
                    AttendanceStatus = AttendanceStatuses.Unmarked
                };

                var reg5 = new EventRegistration
                {
                    UserId = student2.Id,
                    EventId = sampleEvents[2].EventId, // Cultural Fest
                    RegistrationDate = today.AddDays(-2),
                    Status = RegistrationStatus.Approved,
                    AttendanceStatus = AttendanceStatuses.Unmarked
                };

                var reg6 = new EventRegistration
                {
                    UserId = student2.Id,
                    EventId = sampleEvents[9].EventId, // Past Event
                    RegistrationDate = today.AddDays(-21),
                    Status = RegistrationStatus.Approved,
                    AttendanceStatus = AttendanceStatuses.Absent
                };

                context.EventRegistrations.AddRange(reg4, reg5, reg6);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Sample events and registrations seeded successfully.");
        }
    }
}
