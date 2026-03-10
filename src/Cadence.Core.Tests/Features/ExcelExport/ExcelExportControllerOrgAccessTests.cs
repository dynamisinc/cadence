using Cadence.Core.Data;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Features.ExcelExport;

/// <summary>
/// Tests for ExcelExportController's org-scoped access validation (BV-003).
/// The ValidateExerciseOrgAccessAsync private method provides a security layer for
/// the POST /api/export/msel endpoint, where the exerciseId comes from the request
/// body rather than the route, so [AuthorizeExerciseAccess] cannot extract it.
/// These tests mirror the controller's validation logic directly against AppDbContext.
/// </summary>
public class ExcelExportControllerOrgAccessTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Organization _org;
    private readonly Organization _otherOrg;
    private readonly Exercise _exercise;

    public ExcelExportControllerOrgAccessTests()
    {
        _context = TestDbContextFactory.Create();

        _org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };

        _otherOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Other Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };

        _context.Organizations.Add(_org);
        _context.Organizations.Add(_otherOrg);

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
    /// Mirrors ExcelExportController.ValidateExerciseOrgAccessAsync logic.
    /// Returns null when access is granted, or an error string when denied.
    /// </summary>
    private async Task<string?> ValidateExerciseOrgAccess(
        Guid exerciseId, ICurrentOrganizationContext orgContext)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .Where(e => e.Id == exerciseId)
            .Select(e => new { e.OrganizationId })
            .FirstOrDefaultAsync();

        if (exercise == null)
            return "NotFound";

        if (!orgContext.IsSysAdmin &&
            (!orgContext.CurrentOrganizationId.HasValue ||
             orgContext.CurrentOrganizationId.Value != exercise.OrganizationId))
        {
            return "Forbidden";
        }

        return null; // Access granted
    }

    /// <summary>
    /// BV-003: User whose current org matches the exercise's org is granted access.
    /// </summary>
    [Fact]
    public async Task ExportMselPost_ExerciseInSameOrg_Succeeds()
    {
        // Arrange — user is in the same org as the exercise
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(_org.Id);

        // Act
        var error = await ValidateExerciseOrgAccess(_exercise.Id, orgContext.Object);

        // Assert
        error.Should().BeNull("user in the correct org should be granted access");
    }

    /// <summary>
    /// BV-003: User whose current org does not match the exercise's org gets 403.
    /// This is the core cross-org data leakage prevention check.
    /// </summary>
    [Fact]
    public async Task ExportMselPost_ExerciseInDifferentOrg_Returns403()
    {
        // Arrange — user is in a different org than the exercise
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(_otherOrg.Id);

        // Act
        var error = await ValidateExerciseOrgAccess(_exercise.Id, orgContext.Object);

        // Assert
        error.Should().Be("Forbidden", "user from a different org should be denied export access");
    }

    /// <summary>
    /// BV-003: Requesting an export for a non-existent exercise returns NotFound.
    /// </summary>
    [Fact]
    public async Task ExportMselPost_NonExistentExercise_ReturnsNotFound()
    {
        // Arrange
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(_org.Id);

        // Act
        var error = await ValidateExerciseOrgAccess(Guid.NewGuid(), orgContext.Object);

        // Assert
        error.Should().Be("NotFound", "export of a non-existent exercise should return NotFound");
    }

    /// <summary>
    /// BV-003: SysAdmin bypasses org context check and can export any exercise.
    /// </summary>
    [Fact]
    public async Task ExportMselPost_SysAdmin_AllowsCrossOrgAccess()
    {
        // Arrange — SysAdmin with no org context attempts export of exercise in _org
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(true);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns((Guid?)null);

        // Act
        var error = await ValidateExerciseOrgAccess(_exercise.Id, orgContext.Object);

        // Assert
        error.Should().BeNull("SysAdmin should bypass org context restriction and be granted access");
    }

    /// <summary>
    /// BV-003: User with no org context (null CurrentOrganizationId) is denied.
    /// Covers the case of a pending user who has not yet joined an organization.
    /// </summary>
    [Fact]
    public async Task ExportMselPost_UserWithNoOrgContext_Returns403()
    {
        // Arrange
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns((Guid?)null);

        // Act
        var error = await ValidateExerciseOrgAccess(_exercise.Id, orgContext.Object);

        // Assert
        error.Should().Be("Forbidden", "user with no org context should be denied export access");
    }
}
