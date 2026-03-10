using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Exercises.Validators;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Exercises;

public class ExerciseCrudServiceTests
{
    private readonly Mock<ICurrentOrganizationContext> _orgContextMock = new();
    private readonly Mock<IExerciseParticipantService> _participantServiceMock = new();
    private readonly Mock<IMembershipService> _membershipServiceMock = new();
    private readonly Mock<ILogger<ExerciseCrudService>> _loggerMock = new();
    private readonly IValidator<CreateExerciseRequest> _createValidator = new CreateExerciseRequestValidator();

    private (AppDbContext context, Organization org) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Organizations.Add(org);
        context.SaveChanges();

        return (context, org);
    }

    private Exercise CreateExercise(
        AppDbContext context,
        Organization org,
        ExerciseStatus status = ExerciseStatus.Draft,
        string? name = null)
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = name ?? "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = status,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();
        return exercise;
    }

    private ExerciseCrudService CreateService(AppDbContext context)
    {
        return new ExerciseCrudService(
            context,
            _orgContextMock.Object,
            _participantServiceMock.Object,
            _membershipServiceMock.Object,
            _createValidator,
            _loggerMock.Object);
    }

    // =========================================================================
    // GetExercisesAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetExercises_WithOrgContext_ReturnsOnlyOrgExercises()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var org2 = new Organization { Id = Guid.NewGuid(), Name = "Other Org", CreatedBy = "test", ModifiedBy = "test" };
        context.Organizations.Add(org2);
        context.SaveChanges();

        CreateExercise(context, org, name: "Org1 Exercise");
        var exercise2 = new Exercise
        {
            Id = Guid.NewGuid(), Name = "Org2 Exercise", ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft, ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC", OrganizationId = org2.Id, CreatedBy = "test", ModifiedBy = "test"
        };
        context.Exercises.Add(exercise2);
        context.SaveChanges();

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(org.Id);

        var service = CreateService(context);

        // Act
        var result = await service.GetExercisesAsync("user1");

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Org1 Exercise");
    }

    [Fact]
    public async Task GetExercises_NoOrgContext_ReturnsMembershipExercises()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        CreateExercise(context, org, name: "Membership Exercise");

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns((Guid?)null);

        _membershipServiceMock.Setup(x => x.GetUserMembershipsAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MembershipDto(Guid.NewGuid(), "user1", org.Id, "Test Organization", "test-org", "OrgUser", DateTime.UtcNow, false) });

        var service = CreateService(context);

        // Act
        var result = await service.GetExercisesAsync("user1");

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Membership Exercise");
    }

    [Fact]
    public async Task GetExercises_ArchivedOnly_ReturnsOnlyArchived()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        CreateExercise(context, org, ExerciseStatus.Draft, "Draft Exercise");
        CreateExercise(context, org, ExerciseStatus.Archived, "Archived Exercise");

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(org.Id);

        var service = CreateService(context);

        // Act
        var result = await service.GetExercisesAsync("user1", archivedOnly: true);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Archived Exercise");
    }

    [Fact]
    public async Task GetExercises_IncludesInjectCounts()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var msel = new Cadence.Core.Models.Entities.Msel
        {
            Id = Guid.NewGuid(), Name = "v1.0", Version = 1, IsActive = true,
            ExerciseId = exercise.Id, OrganizationId = org.Id,
            CreatedBy = "test", ModifiedBy = "test"
        };
        context.Msels.Add(msel);
        exercise.ActiveMselId = msel.Id;

        var inject1 = new Inject
        {
            Id = Guid.NewGuid(), InjectNumber = 1, Title = "Inject 1",
            Status = InjectStatus.Draft, MselId = msel.Id,
            CreatedBy = "test", ModifiedBy = "test"
        };
        var inject2 = new Inject
        {
            Id = Guid.NewGuid(), InjectNumber = 2, Title = "Inject 2",
            Status = InjectStatus.Released, MselId = msel.Id,
            CreatedBy = "test", ModifiedBy = "test"
        };
        context.Injects.AddRange(inject1, inject2);
        context.SaveChanges();

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(org.Id);

        var service = CreateService(context);

        // Act
        var result = await service.GetExercisesAsync("user1");

        // Assert
        result.Should().HaveCount(1);
        result[0].InjectCount.Should().Be(2);
        result[0].FiredInjectCount.Should().Be(1);
    }

    // =========================================================================
    // CreateExerciseAsync Tests
    // =========================================================================

    [Fact]
    public async Task CreateExercise_ValidRequest_ReturnsDto()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(org.Id);

        var service = CreateService(context);
        var request = new CreateExerciseRequest
        {
            Name = "New Exercise",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC"
        };

        // Act
        var result = await service.CreateExerciseAsync(request, "user1");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Exercise");
        result.Status.Should().Be(ExerciseStatus.Draft);
        result.OrganizationId.Should().Be(org.Id);
    }

    [Fact]
    public async Task CreateExercise_WithDirectorId_AssignsDirector()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(org.Id);

        _participantServiceMock
            .Setup(x => x.AddParticipantAsync(It.IsAny<Guid>(), It.IsAny<AddParticipantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExerciseParticipantDto { UserId = "director1", ExerciseRole = "ExerciseDirector" });

        var service = CreateService(context);
        var request = new CreateExerciseRequest
        {
            Name = "Exercise with Director",
            ExerciseType = ExerciseType.FE,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            DirectorId = "director1"
        };

        // Act
        var result = await service.CreateExerciseAsync(request, "user1");

        // Assert
        result.Should().NotBeNull();
        _participantServiceMock.Verify(x => x.AddParticipantAsync(
            It.IsAny<Guid>(),
            It.Is<AddParticipantRequest>(r => r.UserId == "director1" && r.Role == "ExerciseDirector"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateExercise_NoDirector_AutoAssignsCreatorIfAdmin()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(org.Id);

        // Seed an Admin user
        var adminUser = new ApplicationUser
        {
            Id = "admin1",
            Email = "admin@test.com",
            DisplayName = "Admin User",
            SystemRole = SystemRole.Admin,
            Status = UserStatus.Active
        };
        context.ApplicationUsers.Add(adminUser);
        context.SaveChanges();

        _participantServiceMock
            .Setup(x => x.AddParticipantAsync(It.IsAny<Guid>(), It.IsAny<AddParticipantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExerciseParticipantDto { UserId = "admin1", ExerciseRole = "ExerciseDirector" });

        var service = CreateService(context);
        var request = new CreateExerciseRequest
        {
            Name = "Auto-assign Director",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC"
            // No DirectorId
        };

        // Act
        await service.CreateExerciseAsync(request, "admin1");

        // Assert
        _participantServiceMock.Verify(x => x.AddParticipantAsync(
            It.IsAny<Guid>(),
            It.Is<AddParticipantRequest>(r => r.UserId == "admin1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateExercise_NoOrgContext_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns((Guid?)null);

        var service = CreateService(context);
        var request = new CreateExerciseRequest
        {
            Name = "No Org Exercise",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateExerciseAsync(request, "user1"));
    }

    [Fact]
    public async Task CreateExercise_EmptyName_ThrowsValidationException()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(org.Id);

        var service = CreateService(context);
        var request = new CreateExerciseRequest
        {
            Name = "",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateExerciseAsync(request, "user1"));
    }

    // =========================================================================
    // UpdateExerciseAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdateExercise_ValidRequest_UpdatesFields()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var service = CreateService(context);

        var request = new UpdateExerciseRequest
        {
            Name = "Updated Name",
            ExerciseType = ExerciseType.FE,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            TimeZoneId = "America/New_York"
        };

        // Act
        var result = await service.UpdateExerciseAsync(exercise.Id, request, "user1");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.ExerciseType.Should().Be(ExerciseType.FE);
        result.TimeZoneId.Should().Be("America/New_York");
    }

    [Fact]
    public async Task UpdateExercise_CompletedExercise_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Completed);
        var service = CreateService(context);

        var request = new UpdateExerciseRequest
        {
            Name = "Updated",
            TimeZoneId = "UTC"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateExerciseAsync(exercise.Id, request, "user1"));
    }

    [Fact]
    public async Task UpdateExercise_ArchivedExercise_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Archived);
        var service = CreateService(context);

        var request = new UpdateExerciseRequest
        {
            Name = "Updated",
            TimeZoneId = "UTC"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateExerciseAsync(exercise.Id, request, "user1"));
    }

    [Fact]
    public async Task UpdateExercise_NotFound_ReturnsNull()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.UpdateExerciseAsync(Guid.NewGuid(), new UpdateExerciseRequest { Name = "x", TimeZoneId = "UTC" }, "user1");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateExercise_DirectorReassignment_SwapsDirector()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        _participantServiceMock
            .Setup(x => x.GetParticipantsAsync(exercise.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExerciseParticipantDto>
            {
                new() { UserId = "old-director", ExerciseRole = "ExerciseDirector" }
            });

        var service = CreateService(context);
        var request = new UpdateExerciseRequest
        {
            Name = "Updated",
            TimeZoneId = "UTC",
            DirectorId = "new-director"
        };

        // Act
        await service.UpdateExerciseAsync(exercise.Id, request, "user1");

        // Assert
        _participantServiceMock.Verify(x => x.RemoveParticipantAsync(exercise.Id, "old-director", It.IsAny<CancellationToken>()), Times.Once);
        _participantServiceMock.Verify(x => x.AddParticipantAsync(
            exercise.Id,
            It.Is<AddParticipantRequest>(r => r.UserId == "new-director" && r.Role == "ExerciseDirector"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // DuplicateExerciseAsync Tests
    // =========================================================================

    [Fact]
    public async Task DuplicateExercise_CopiesAllData_ResetsStatusToDraft()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var source = CreateExercise(context, org, ExerciseStatus.Active, "Source Exercise");

        var msel = new Cadence.Core.Models.Entities.Msel
        {
            Id = Guid.NewGuid(), Name = "v1.0", Version = 1, IsActive = true,
            ExerciseId = source.Id, OrganizationId = org.Id,
            CreatedBy = "test", ModifiedBy = "test"
        };
        context.Msels.Add(msel);
        source.ActiveMselId = msel.Id;

        var inject = new Inject
        {
            Id = Guid.NewGuid(), InjectNumber = 1, Title = "Test Inject",
            Status = InjectStatus.Released, Sequence = 1, MselId = msel.Id,
            CreatedBy = "test", ModifiedBy = "test"
        };
        context.Injects.Add(inject);
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = await service.DuplicateExerciseAsync(source.Id, null, "test-user");

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ExerciseStatus.Draft);
        result.Name.Should().Be("Copy of Source Exercise");
        result.Id.Should().NotBe(source.Id);
        result.ActiveMselId.Should().NotBeNull();
        result.ActiveMselId.Should().NotBe(msel.Id);

        // Verify the duplicated inject is Draft (not Released)
        var newInjects = context.Injects.Where(i => i.MselId == result.ActiveMselId).ToList();
        newInjects.Should().HaveCount(1);
        newInjects[0].Status.Should().Be(InjectStatus.Draft);
        newInjects[0].Title.Should().Be("Test Inject");
    }

    [Fact]
    public async Task DuplicateExercise_RemapsPhaseAndObjectiveIds()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var source = CreateExercise(context, org);

        var phase = new Phase
        {
            Id = Guid.NewGuid(), Name = "Phase 1", Sequence = 1,
            ExerciseId = source.Id, OrganizationId = org.Id,
            CreatedBy = "test", ModifiedBy = "test"
        };
        context.Phases.Add(phase);

        var objective = new Objective
        {
            Id = Guid.NewGuid(), ObjectiveNumber = "1", Name = "Objective 1",
            ExerciseId = source.Id, OrganizationId = org.Id,
            CreatedBy = "test", ModifiedBy = "test"
        };
        context.Objectives.Add(objective);

        var msel = new Cadence.Core.Models.Entities.Msel
        {
            Id = Guid.NewGuid(), Name = "v1.0", Version = 1, IsActive = true,
            ExerciseId = source.Id, OrganizationId = org.Id,
            CreatedBy = "test", ModifiedBy = "test"
        };
        context.Msels.Add(msel);
        source.ActiveMselId = msel.Id;

        var inject = new Inject
        {
            Id = Guid.NewGuid(), InjectNumber = 1, Title = "Inject with Phase",
            Status = InjectStatus.Draft, Sequence = 1, MselId = msel.Id,
            PhaseId = phase.Id,
            CreatedBy = "test", ModifiedBy = "test"
        };
        context.Injects.Add(inject);

        context.InjectObjectives.Add(new InjectObjective
        {
            InjectId = inject.Id,
            ObjectiveId = objective.Id
        });
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = await service.DuplicateExerciseAsync(source.Id, null, "test-user");

        // Assert
        result.Should().NotBeNull();

        // Verify phases were duplicated with new IDs
        var newPhases = context.Phases.Where(p => p.ExerciseId == result!.Id).ToList();
        newPhases.Should().HaveCount(1);
        newPhases[0].Id.Should().NotBe(phase.Id);
        newPhases[0].Name.Should().Be("Phase 1");

        // Verify objectives were duplicated with new IDs
        var newObjectives = context.Objectives.Where(o => o.ExerciseId == result!.Id).ToList();
        newObjectives.Should().HaveCount(1);
        newObjectives[0].Id.Should().NotBe(objective.Id);

        // Verify inject references the NEW phase ID
        var newInjects = context.Injects.Where(i => i.MselId == result!.ActiveMselId).ToList();
        newInjects.Should().HaveCount(1);
        newInjects[0].PhaseId.Should().Be(newPhases[0].Id);

        // Verify inject-objective link references the NEW objective ID
        var newLinks = context.InjectObjectives.Where(io => io.InjectId == newInjects[0].Id).ToList();
        newLinks.Should().HaveCount(1);
        newLinks[0].ObjectiveId.Should().Be(newObjectives[0].Id);
    }

    [Fact]
    public async Task DuplicateExercise_NotFound_ReturnsNull()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.DuplicateExerciseAsync(Guid.NewGuid(), null, "test-user");

        // Assert
        result.Should().BeNull();
    }
}
