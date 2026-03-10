using Cadence.Core.Data;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Features.Authorization;

/// <summary>
/// Tests for AutocompleteController's org-scoped access validation (AC-M06).
/// The ValidateExerciseAccessAsync private method provides an additional security layer
/// beyond [AuthorizeExerciseAccess] by verifying the user's current organization context
/// matches the exercise's organization. This prevents cross-org data leakage even when
/// a user has exercise participation rights.
/// </summary>
public class AutocompleteOrgAccessTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Organization _org;
    private readonly Exercise _exercise;

    public AutocompleteOrgAccessTests()
    {
        _context = TestDbContextFactory.Create();

        _org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        _context.Organizations.Add(_org);

        _exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = _org.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        _context.Exercises.Add(_exercise);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    /// <summary>
    /// Mirrors AutocompleteController.ValidateExerciseAccessAsync logic.
    /// Returns (orgId, error) where error is non-null if access is denied.
    /// </summary>
    private async Task<(Guid? OrgId, string? Error)> ValidateExerciseAccess(
        Guid exerciseId, ICurrentOrganizationContext orgContext)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null)
            return (null, "NotFound");

        var organizationId = exercise.OrganizationId;

        // SysAdmins can access any organization
        if (orgContext.IsSysAdmin)
            return (organizationId, null);

        // Regular users must have a current organization context matching the exercise's org
        if (!orgContext.CurrentOrganizationId.HasValue ||
            orgContext.CurrentOrganizationId.Value != organizationId)
        {
            return (null, "Forbid");
        }

        return (organizationId, null);
    }

    /// <summary>
    /// AC-M06: User whose current org does not match the exercise's org gets Forbid.
    /// Guards against cross-org autocomplete data leakage.
    /// </summary>
    [Fact]
    public async Task ValidateExerciseAccess_UserInDifferentOrg_ReturnsForbid()
    {
        // Arrange — user is in a different org than the exercise
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(Guid.NewGuid());

        // Act
        var (orgId, error) = await ValidateExerciseAccess(_exercise.Id, orgContext.Object);

        // Assert
        error.Should().Be("Forbid");
        orgId.Should().BeNull();
    }

    /// <summary>
    /// AC-M06: SysAdmin bypasses org context check — can access any exercise's data.
    /// </summary>
    [Fact]
    public async Task ValidateExerciseAccess_SysAdmin_ReturnsOrganizationId()
    {
        // Arrange
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(true);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns((Guid?)null);

        // Act
        var (orgId, error) = await ValidateExerciseAccess(_exercise.Id, orgContext.Object);

        // Assert
        error.Should().BeNull("SysAdmin should bypass org context restriction");
        orgId.Should().Be(_org.Id);
    }

    /// <summary>
    /// AC-M06: User in the matching org gets access.
    /// </summary>
    [Fact]
    public async Task ValidateExerciseAccess_UserInMatchingOrg_ReturnsOrganizationId()
    {
        // Arrange
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(_org.Id);

        // Act
        var (orgId, error) = await ValidateExerciseAccess(_exercise.Id, orgContext.Object);

        // Assert
        error.Should().BeNull("user in the correct org should be granted access");
        orgId.Should().Be(_org.Id);
    }

    /// <summary>
    /// AC-M06: Non-existent exercise returns NotFound.
    /// </summary>
    [Fact]
    public async Task ValidateExerciseAccess_ExerciseNotFound_ReturnsNotFound()
    {
        // Arrange
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(Guid.NewGuid());

        // Act
        var (orgId, error) = await ValidateExerciseAccess(Guid.NewGuid(), orgContext.Object);

        // Assert
        error.Should().Be("NotFound");
        orgId.Should().BeNull();
    }

    /// <summary>
    /// AC-M06: User with no org context (null CurrentOrganizationId) gets Forbid.
    /// </summary>
    [Fact]
    public async Task ValidateExerciseAccess_UserWithNoOrgContext_ReturnsForbid()
    {
        // Arrange
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns((Guid?)null);

        // Act
        var (orgId, error) = await ValidateExerciseAccess(_exercise.Id, orgContext.Object);

        // Assert
        error.Should().Be("Forbid");
        orgId.Should().BeNull();
    }
}
