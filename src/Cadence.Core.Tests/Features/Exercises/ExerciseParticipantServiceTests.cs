using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Exercises;

/// <summary>
/// Tests for ExerciseParticipantService — CRUD for exercise participants and roles.
/// </summary>
public class ExerciseParticipantServiceTests
{
    private readonly Mock<ILogger<ExerciseParticipantService>> _loggerMock = new();

    private (AppDbContext context, ExerciseParticipantService service, Exercise exercise) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            Slug = "test-org"
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            OrganizationId = org.Id,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        var service = new ExerciseParticipantService(context, _loggerMock.Object);
        return (context, service, exercise);
    }

    private ApplicationUser CreateUser(AppDbContext context, SystemRole role = SystemRole.User, string? displayName = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"user-{Guid.NewGuid():N}@test.com",
            Email = $"user-{Guid.NewGuid():N}@test.com",
            DisplayName = displayName ?? "Test User",
            SystemRole = role
        };
        context.ApplicationUsers.Add(user);
        context.SaveChanges();
        return user;
    }

    // =========================================================================
    // GetParticipantsAsync
    // =========================================================================

    [Fact]
    public async Task GetParticipantsAsync_ReturnsAllActiveParticipants()
    {
        var (context, service, exercise) = CreateTestContext();
        var user1 = CreateUser(context, displayName: "Alice");
        var user2 = CreateUser(context, displayName: "Bob");

        context.ExerciseParticipants.AddRange(
            new ExerciseParticipant { ExerciseId = exercise.Id, UserId = user1.Id, Role = ExerciseRole.Controller, AssignedAt = DateTime.UtcNow },
            new ExerciseParticipant { ExerciseId = exercise.Id, UserId = user2.Id, Role = ExerciseRole.Evaluator, AssignedAt = DateTime.UtcNow }
        );
        context.SaveChanges();

        var result = await service.GetParticipantsAsync(exercise.Id);

        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(p => p.DisplayName);
    }

    [Fact]
    public async Task GetParticipantsAsync_ExcludesSoftDeleted()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context);

        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = user.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var result = await service.GetParticipantsAsync(exercise.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetParticipantsAsync_NoParticipants_ReturnsEmptyList()
    {
        var (_, service, exercise) = CreateTestContext();

        var result = await service.GetParticipantsAsync(exercise.Id);

        result.Should().BeEmpty();
    }

    // =========================================================================
    // GetParticipantAsync
    // =========================================================================

    [Fact]
    public async Task GetParticipantAsync_ExistingParticipant_ReturnsDto()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context, displayName: "Jane");

        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = user.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var result = await service.GetParticipantAsync(exercise.Id, user.Id);

        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Jane");
        result.ExerciseRole.Should().Be("Controller");
    }

    [Fact]
    public async Task GetParticipantAsync_Nonexistent_ReturnsNull()
    {
        var (_, service, exercise) = CreateTestContext();

        var result = await service.GetParticipantAsync(exercise.Id, "nonexistent-user");

        result.Should().BeNull();
    }

    // =========================================================================
    // GetEffectiveRoleAsync
    // =========================================================================

    [Fact]
    public async Task GetEffectiveRoleAsync_Participant_ReturnsAssignedRole()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context);

        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = user.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var result = await service.GetEffectiveRoleAsync(exercise.Id, user.Id);

        result.Should().Be("Controller");
    }

    [Fact]
    public async Task GetEffectiveRoleAsync_NonParticipant_ReturnsObserver()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context);

        var result = await service.GetEffectiveRoleAsync(exercise.Id, user.Id);

        result.Should().Be("Observer");
    }

    // =========================================================================
    // AddParticipantAsync
    // =========================================================================

    [Fact]
    public async Task AddParticipantAsync_ValidRequest_CreatesParticipant()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context);
        var request = new AddParticipantRequest { UserId = user.Id, Role = "Controller" };

        var result = await service.AddParticipantAsync(exercise.Id, request);

        result.Should().NotBeNull();
        result.ExerciseRole.Should().Be("Controller");
        result.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task AddParticipantAsync_DuplicateUser_ThrowsInvalidOperationException()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context);

        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = user.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var request = new AddParticipantRequest { UserId = user.Id, Role = "Evaluator" };
        var act = () => service.AddParticipantAsync(exercise.Id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already a participant*");
    }

    [Fact]
    public async Task AddParticipantAsync_ReactivatesSoftDeletedParticipant()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context);

        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = user.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var request = new AddParticipantRequest { UserId = user.Id, Role = "Evaluator" };
        var result = await service.AddParticipantAsync(exercise.Id, request);

        result.Should().NotBeNull();
        result.ExerciseRole.Should().Be("Evaluator");
    }

    [Fact]
    public async Task AddParticipantAsync_NonexistentUser_ThrowsKeyNotFoundException()
    {
        var (_, service, exercise) = CreateTestContext();
        var request = new AddParticipantRequest { UserId = "nonexistent", Role = "Controller" };

        var act = () => service.AddParticipantAsync(exercise.Id, request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AddParticipantAsync_ExerciseDirector_RequiresAdminOrManager()
    {
        var (context, service, exercise) = CreateTestContext();
        var regularUser = CreateUser(context, SystemRole.User);
        var request = new AddParticipantRequest { UserId = regularUser.Id, Role = "ExerciseDirector" };

        var act = () => service.AddParticipantAsync(exercise.Id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Admins and Managers*");
    }

    [Fact]
    public async Task AddParticipantAsync_ExerciseDirector_ManagerAllowed()
    {
        var (context, service, exercise) = CreateTestContext();
        var manager = CreateUser(context, SystemRole.Manager);
        var request = new AddParticipantRequest { UserId = manager.Id, Role = "ExerciseDirector" };

        var result = await service.AddParticipantAsync(exercise.Id, request);

        result.ExerciseRole.Should().Be("ExerciseDirector");
    }

    [Fact]
    public async Task AddParticipantAsync_InvalidRole_ThrowsArgumentException()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context);
        var request = new AddParticipantRequest { UserId = user.Id, Role = "InvalidRole" };

        var act = () => service.AddParticipantAsync(exercise.Id, request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid role*");
    }

    [Fact]
    public async Task AddParticipantAsync_NullRole_DefaultsToObserver()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context);
        var request = new AddParticipantRequest { UserId = user.Id, Role = null! };

        var result = await service.AddParticipantAsync(exercise.Id, request);

        result.ExerciseRole.Should().Be("Observer");
    }

    // =========================================================================
    // UpdateParticipantRoleAsync
    // =========================================================================

    [Fact]
    public async Task UpdateParticipantRoleAsync_ValidRequest_UpdatesRole()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context, SystemRole.Manager);

        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = user.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var request = new UpdateParticipantRoleRequest { Role = "ExerciseDirector" };
        var result = await service.UpdateParticipantRoleAsync(exercise.Id, user.Id, request);

        result.ExerciseRole.Should().Be("ExerciseDirector");
    }

    [Fact]
    public async Task UpdateParticipantRoleAsync_NonexistentParticipant_ThrowsKeyNotFoundException()
    {
        var (_, service, exercise) = CreateTestContext();
        var request = new UpdateParticipantRoleRequest { Role = "Controller" };

        var act = () => service.UpdateParticipantRoleAsync(exercise.Id, "nonexistent", request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateParticipantRoleAsync_ExerciseDirector_RegularUser_ThrowsInvalidOperation()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context, SystemRole.User);

        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = user.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var request = new UpdateParticipantRoleRequest { Role = "ExerciseDirector" };
        var act = () => service.UpdateParticipantRoleAsync(exercise.Id, user.Id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Admins and Managers*");
    }

    // =========================================================================
    // RemoveParticipantAsync
    // =========================================================================

    [Fact]
    public async Task RemoveParticipantAsync_ExistingParticipant_SoftDeletes()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context);

        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = user.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        await service.RemoveParticipantAsync(exercise.Id, user.Id);

        var participant = await service.GetParticipantAsync(exercise.Id, user.Id);
        participant.Should().BeNull();
    }

    [Fact]
    public async Task RemoveParticipantAsync_NonexistentParticipant_ThrowsKeyNotFoundException()
    {
        var (_, service, exercise) = CreateTestContext();

        var act = () => service.RemoveParticipantAsync(exercise.Id, "nonexistent");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // =========================================================================
    // BulkUpdateParticipantsAsync
    // =========================================================================

    [Fact]
    public async Task BulkUpdateParticipantsAsync_AddsNewAndUpdatesExisting()
    {
        var (context, service, exercise) = CreateTestContext();
        var user1 = CreateUser(context, displayName: "User1");
        var user2 = CreateUser(context, displayName: "User2");

        // Add user1 as existing participant
        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = user1.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var request = new BulkUpdateParticipantsRequest
        {
            Participants = new List<AddParticipantRequest>
            {
                new() { UserId = user1.Id, Role = "Evaluator" },  // Update existing
                new() { UserId = user2.Id, Role = "Observer" }     // Add new
            }
        };

        await service.BulkUpdateParticipantsAsync(exercise.Id, request);

        var participants = await service.GetParticipantsAsync(exercise.Id);
        participants.Should().HaveCount(2);
        participants.First(p => p.UserId == user1.Id).ExerciseRole.Should().Be("Evaluator");
        participants.First(p => p.UserId == user2.Id).ExerciseRole.Should().Be("Observer");
    }

    // =========================================================================
    // GetUserExerciseAssignmentsAsync
    // =========================================================================

    [Fact]
    public async Task GetUserExerciseAssignmentsAsync_ReturnsUserAssignments()
    {
        var (context, service, exercise) = CreateTestContext();
        var user = CreateUser(context);

        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = user.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var result = await service.GetUserExerciseAssignmentsAsync(user.Id);

        result.Should().HaveCount(1);
        var assignment = result.First();
        assignment.ExerciseName.Should().Be("Test Exercise");
        assignment.ExerciseRole.Should().Be("Controller");
    }

    [Fact]
    public async Task GetUserExerciseAssignmentsAsync_ExcludesSoftDeletedExercises()
    {
        var (context, service, _) = CreateTestContext();
        var user = CreateUser(context);

        // Create a soft-deleted exercise
        var deletedExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Exercise",
            OrganizationId = context.Organizations.First().Id,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        context.Exercises.Add(deletedExercise);

        context.ExerciseParticipants.Add(new ExerciseParticipant
        {
            ExerciseId = deletedExercise.Id,
            UserId = user.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var result = await service.GetUserExerciseAssignmentsAsync(user.Id);

        result.Should().BeEmpty();
    }
}
