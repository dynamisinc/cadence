using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;

namespace Cadence.Core.Tests.Features.Exercises;

/// <summary>
/// Tests for ApprovalPermissionService (S11: Configurable Approval Permissions).
/// </summary>
public class ApprovalPermissionServiceTests
{
    private (AppDbContext context, ApprovalPermissionService service, Organization org, Exercise exercise, Msel msel) CreateTestContext(
        ApprovalRoles authorizedRoles = ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector,
        SelfApprovalPolicy selfApprovalPolicy = SelfApprovalPolicy.NeverAllowed)
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            Slug = "test-org",
            ApprovalAuthorizedRoles = authorizedRoles,
            SelfApprovalPolicy = selfApprovalPolicy
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            OrganizationId = org.Id,
            Organization = org,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        context.Exercises.Add(exercise);

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            ExerciseId = exercise.Id,
            Exercise = exercise,
            OrganizationId = org.Id
        };
        context.Msels.Add(msel);

        context.SaveChanges();

        var service = new ApprovalPermissionService(context);
        return (context, service, org, exercise, msel);
    }

    private ApplicationUser CreateUser(AppDbContext context, SystemRole systemRole = SystemRole.User)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"user-{Guid.NewGuid():N}@test.com",
            Email = $"user-{Guid.NewGuid():N}@test.com",
            DisplayName = "Test User",
            SystemRole = systemRole
        };
        context.ApplicationUsers.Add(user);
        context.SaveChanges();
        return user;
    }

    private ExerciseParticipant AddParticipant(AppDbContext context, Guid exerciseId, string userId, ExerciseRole role)
    {
        var participant = new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            UserId = userId,
            Role = role,
            AssignedAt = DateTime.UtcNow
        };
        context.ExerciseParticipants.Add(participant);
        context.SaveChanges();
        return participant;
    }

    private Inject CreateInject(AppDbContext context, Guid mselId, string? submittedByUserId = null)
    {
        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            MselId = mselId,
            Title = "Test Inject",
            Description = "Test Description",
            Target = "EOC Team",
            InjectNumber = 1,
            SubmittedByUserId = submittedByUserId
        };
        context.Injects.Add(inject);
        context.SaveChanges();
        return inject;
    }

    // =========================================================================
    // GetApprovalPermissionsAsync
    // =========================================================================

    [Fact]
    public async Task GetApprovalPermissionsAsync_ValidOrg_ReturnsPermissions()
    {
        var (context, service, org, _, _) = CreateTestContext();

        var result = await service.GetApprovalPermissionsAsync(org.Id);

        result.AuthorizedRoles.Should().Be(ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector);
        result.SelfApprovalPolicy.Should().Be(SelfApprovalPolicy.NeverAllowed);
        result.AuthorizedRoleNames.Should().Contain("Administrator");
        result.AuthorizedRoleNames.Should().Contain("Exercise Director");
    }

    [Fact]
    public async Task GetApprovalPermissionsAsync_NonexistentOrg_ThrowsKeyNotFoundException()
    {
        var (_, service, _, _, _) = CreateTestContext();

        var act = () => service.GetApprovalPermissionsAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // =========================================================================
    // UpdateApprovalPermissionsAsync
    // =========================================================================

    [Fact]
    public async Task UpdateApprovalPermissionsAsync_AddsControllerRole_PersistsChange()
    {
        var (context, service, org, _, _) = CreateTestContext();
        var request = new UpdateApprovalPermissionsRequest(
            ApprovalRoles.Administrator | ApprovalRoles.Controller,
            SelfApprovalPolicy.AllowedWithWarning);

        var result = await service.UpdateApprovalPermissionsAsync(org.Id, request, "user-1");

        result.AuthorizedRoles.Should().HaveFlag(ApprovalRoles.Administrator);
        result.AuthorizedRoles.Should().HaveFlag(ApprovalRoles.Controller);
        result.SelfApprovalPolicy.Should().Be(SelfApprovalPolicy.AllowedWithWarning);
        result.AuthorizedRoleNames.Should().Contain("Controller");
    }

    [Fact]
    public async Task UpdateApprovalPermissionsAsync_RemovingAdministrator_StillIncludesAdministrator()
    {
        var (_, service, org, _, _) = CreateTestContext();
        var request = new UpdateApprovalPermissionsRequest(
            ApprovalRoles.Controller, // Trying to remove Administrator
            SelfApprovalPolicy.NeverAllowed);

        var result = await service.UpdateApprovalPermissionsAsync(org.Id, request, "user-1");

        // Administrator is always forced in
        result.AuthorizedRoles.Should().HaveFlag(ApprovalRoles.Administrator);
        result.AuthorizedRoles.Should().HaveFlag(ApprovalRoles.Controller);
    }

    [Fact]
    public async Task UpdateApprovalPermissionsAsync_NonexistentOrg_ThrowsKeyNotFoundException()
    {
        var (_, service, _, _, _) = CreateTestContext();
        var request = new UpdateApprovalPermissionsRequest(ApprovalRoles.Administrator, SelfApprovalPolicy.NeverAllowed);

        var act = () => service.UpdateApprovalPermissionsAsync(Guid.NewGuid(), request, "user-1");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // =========================================================================
    // CanApproveAsync
    // =========================================================================

    [Fact]
    public async Task CanApproveAsync_AuthorizedRole_ReturnsTrue()
    {
        var (context, service, _, exercise, _) = CreateTestContext();
        var user = CreateUser(context);
        AddParticipant(context, exercise.Id, user.Id, ExerciseRole.ExerciseDirector);

        var result = await service.CanApproveAsync(user.Id, exercise.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanApproveAsync_UnauthorizedRole_ReturnsFalse()
    {
        var (context, service, _, exercise, _) = CreateTestContext();
        var user = CreateUser(context);
        AddParticipant(context, exercise.Id, user.Id, ExerciseRole.Evaluator);

        var result = await service.CanApproveAsync(user.Id, exercise.Id);

        result.Should().BeFalse(); // Default: only Admin + ExerciseDirector
    }

    [Fact]
    public async Task CanApproveAsync_SystemAdmin_WithoutParticipation_ReturnsTrue()
    {
        var (context, service, _, exercise, _) = CreateTestContext();
        var admin = CreateUser(context, SystemRole.Admin);

        var result = await service.CanApproveAsync(admin.Id, exercise.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanApproveAsync_NonParticipantRegularUser_ReturnsFalse()
    {
        var (context, service, _, exercise, _) = CreateTestContext();
        var user = CreateUser(context);

        var result = await service.CanApproveAsync(user.Id, exercise.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanApproveAsync_ControllerRole_WhenControllerAuthorized_ReturnsTrue()
    {
        var (context, service, _, exercise, _) = CreateTestContext(
            authorizedRoles: ApprovalRoles.Administrator | ApprovalRoles.Controller);
        var user = CreateUser(context);
        AddParticipant(context, exercise.Id, user.Id, ExerciseRole.Controller);

        var result = await service.CanApproveAsync(user.Id, exercise.Id);

        result.Should().BeTrue();
    }

    // =========================================================================
    // CanApproveInjectAsync
    // =========================================================================

    [Fact]
    public async Task CanApproveInjectAsync_AuthorizedNotSelfApproval_ReturnsAllowed()
    {
        var (context, service, _, exercise, msel) = CreateTestContext();
        var submitter = CreateUser(context);
        var approver = CreateUser(context);
        AddParticipant(context, exercise.Id, approver.Id, ExerciseRole.ExerciseDirector);
        var inject = CreateInject(context, msel.Id, submitter.Id);

        var result = await service.CanApproveInjectAsync(approver.Id, inject.Id);

        result.CanApprove.Should().BeTrue();
        result.PermissionResult.Should().Be(ApprovalPermissionResult.Allowed);
        result.IsSelfApproval.Should().BeFalse();
        result.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public async Task CanApproveInjectAsync_UnauthorizedRole_ReturnsNotAuthorized()
    {
        var (context, service, _, exercise, msel) = CreateTestContext();
        var user = CreateUser(context);
        // Evaluator is a participant but NOT in the default authorized roles (Admin + ExerciseDirector)
        AddParticipant(context, exercise.Id, user.Id, ExerciseRole.Evaluator);
        var inject = CreateInject(context, msel.Id);

        var result = await service.CanApproveInjectAsync(user.Id, inject.Id);

        result.CanApprove.Should().BeFalse();
        result.PermissionResult.Should().Be(ApprovalPermissionResult.NotAuthorized);
    }

    [Fact]
    public async Task CanApproveInjectAsync_SelfApproval_NeverAllowed_ReturnsDenied()
    {
        var (context, service, _, exercise, msel) = CreateTestContext(
            selfApprovalPolicy: SelfApprovalPolicy.NeverAllowed);
        var user = CreateUser(context);
        AddParticipant(context, exercise.Id, user.Id, ExerciseRole.ExerciseDirector);
        var inject = CreateInject(context, msel.Id, user.Id);

        var result = await service.CanApproveInjectAsync(user.Id, inject.Id);

        result.CanApprove.Should().BeFalse();
        result.PermissionResult.Should().Be(ApprovalPermissionResult.SelfApprovalDenied);
        result.IsSelfApproval.Should().BeTrue();
    }

    [Fact]
    public async Task CanApproveInjectAsync_SelfApproval_AllowedWithWarning_ReturnsWithConfirmation()
    {
        var (context, service, _, exercise, msel) = CreateTestContext(
            selfApprovalPolicy: SelfApprovalPolicy.AllowedWithWarning);
        var user = CreateUser(context);
        AddParticipant(context, exercise.Id, user.Id, ExerciseRole.ExerciseDirector);
        var inject = CreateInject(context, msel.Id, user.Id);

        var result = await service.CanApproveInjectAsync(user.Id, inject.Id);

        result.CanApprove.Should().BeTrue();
        result.PermissionResult.Should().Be(ApprovalPermissionResult.SelfApprovalWithWarning);
        result.IsSelfApproval.Should().BeTrue();
        result.RequiresConfirmation.Should().BeTrue();
    }

    [Fact]
    public async Task CanApproveInjectAsync_SelfApproval_AlwaysAllowed_ReturnsAllowed()
    {
        var (context, service, _, exercise, msel) = CreateTestContext(
            selfApprovalPolicy: SelfApprovalPolicy.AlwaysAllowed);
        var user = CreateUser(context);
        AddParticipant(context, exercise.Id, user.Id, ExerciseRole.ExerciseDirector);
        var inject = CreateInject(context, msel.Id, user.Id);

        var result = await service.CanApproveInjectAsync(user.Id, inject.Id);

        result.CanApprove.Should().BeTrue();
        result.PermissionResult.Should().Be(ApprovalPermissionResult.Allowed);
        result.IsSelfApproval.Should().BeTrue();
        result.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public async Task CanApproveInjectAsync_NonexistentInject_ThrowsKeyNotFoundException()
    {
        var (_, service, _, _, _) = CreateTestContext();

        var act = () => service.CanApproveInjectAsync("user-1", Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // =========================================================================
    // GetApprovalRoleFlag
    // =========================================================================

    [Theory]
    [InlineData(ExerciseRole.Administrator, ApprovalRoles.Administrator)]
    [InlineData(ExerciseRole.ExerciseDirector, ApprovalRoles.ExerciseDirector)]
    [InlineData(ExerciseRole.Controller, ApprovalRoles.Controller)]
    [InlineData(ExerciseRole.Evaluator, ApprovalRoles.Evaluator)]
    [InlineData(ExerciseRole.Observer, ApprovalRoles.None)]
    public void GetApprovalRoleFlag_MapsCorrectly(ExerciseRole role, ApprovalRoles expected)
    {
        var context = TestDbContextFactory.Create();
        var service = new ApprovalPermissionService(context);

        var result = service.GetApprovalRoleFlag(role);

        result.Should().Be(expected);
    }

    // =========================================================================
    // GetRoleNames
    // =========================================================================

    [Fact]
    public void GetRoleNames_AllFlags_ReturnsAllNames()
    {
        var context = TestDbContextFactory.Create();
        var service = new ApprovalPermissionService(context);
        var allRoles = ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector |
                       ApprovalRoles.Controller | ApprovalRoles.Evaluator;

        var names = service.GetRoleNames(allRoles);

        names.Should().HaveCount(4);
        names.Should().Contain("Administrator");
        names.Should().Contain("Exercise Director");
        names.Should().Contain("Controller");
        names.Should().Contain("Evaluator");
    }

    [Fact]
    public void GetRoleNames_NoFlags_ReturnsEmpty()
    {
        var context = TestDbContextFactory.Create();
        var service = new ApprovalPermissionService(context);

        var names = service.GetRoleNames(ApprovalRoles.None);

        names.Should().BeEmpty();
    }
}
