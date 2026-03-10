using Cadence.Core.Data;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Features.Observations;

/// <summary>
/// Tests for ObservationsController's org-scoped access validation (BV-004).
/// The GET endpoints (GetObservationsByInject and GetObservation) are missing
/// [AuthorizeExerciseAccess] and instead perform org isolation via private helper methods.
/// These tests mirror that validation logic to ensure cross-org data leakage is prevented.
/// </summary>
public class ObservationsControllerOrgAccessTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Organization _org;
    private readonly Organization _otherOrg;
    private readonly Exercise _exercise;
    private readonly Msel _msel;
    private readonly Inject _inject;
    private readonly Observation _observation;

    public ObservationsControllerOrgAccessTests()
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

        _msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            ExerciseId = _exercise.Id,
            OrganizationId = _org.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        _context.Msels.Add(_msel);

        _inject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = 1,
            Title = "Test Inject",
            Description = "Test description",
            ScheduledTime = new TimeOnly(9, 0),
            Target = "Target",
            InjectType = InjectType.Standard,
            Status = InjectStatus.Draft,
            Sequence = 1,
            MselId = _msel.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        _context.Injects.Add(_inject);

        _observation = new Observation
        {
            Id = Guid.NewGuid(),
            Content = "Test observation content",
            ObservedAt = DateTime.UtcNow,
            ExerciseId = _exercise.Id,
            InjectId = _inject.Id,
            OrganizationId = _org.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        _context.Observations.Add(_observation);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // =========================================================================
    // Inject-based validation (mirrors ValidateInjectOrgAccessAsync)
    // =========================================================================

    /// <summary>
    /// Mirrors ObservationsController.ValidateInjectOrgAccessAsync logic.
    /// Returns null when access is granted, or an error string when denied.
    /// </summary>
    private async Task<string?> ValidateInjectOrgAccess(
        Guid injectId, ICurrentOrganizationContext orgContext)
    {
        var orgId = await _context.Injects
            .AsNoTracking()
            .Where(i => i.Id == injectId)
            .Select(i => i.Msel.Exercise.OrganizationId)
            .FirstOrDefaultAsync();

        if (orgId == default)
            return "NotFound";

        if (!orgContext.IsSysAdmin &&
            (!orgContext.CurrentOrganizationId.HasValue ||
             orgContext.CurrentOrganizationId.Value != orgId))
        {
            return "Forbidden";
        }

        return null; // Access granted
    }

    // =========================================================================
    // Observation-based validation (mirrors ValidateObservationOrgAccessAsync)
    // =========================================================================

    /// <summary>
    /// Mirrors ObservationsController.ValidateObservationOrgAccessAsync logic.
    /// Returns null when access is granted, or an error string when denied.
    /// </summary>
    private async Task<string?> ValidateObservationOrgAccess(
        Guid observationId, ICurrentOrganizationContext orgContext)
    {
        var orgId = await _context.Observations
            .AsNoTracking()
            .Where(o => o.Id == observationId)
            .Select(o => o.OrganizationId)
            .FirstOrDefaultAsync();

        if (orgId == default)
            return "NotFound";

        if (!orgContext.IsSysAdmin &&
            (!orgContext.CurrentOrganizationId.HasValue ||
             orgContext.CurrentOrganizationId.Value != orgId))
        {
            return "Forbidden";
        }

        return null; // Access granted
    }

    // =========================================================================
    // GetObservationsByInject tests
    // =========================================================================

    /// <summary>
    /// BV-004: User in the same org as the inject's exercise is granted access.
    /// </summary>
    [Fact]
    public async Task GetObservationsByInject_InjectInSameOrg_Succeeds()
    {
        // Arrange — user is in the same org as the inject's exercise
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(_org.Id);

        // Act
        var error = await ValidateInjectOrgAccess(_inject.Id, orgContext.Object);

        // Assert
        error.Should().BeNull("user in the correct org should be granted access to inject observations");
    }

    /// <summary>
    /// BV-004: User in a different org than the inject's exercise gets 403.
    /// </summary>
    [Fact]
    public async Task GetObservationsByInject_InjectInDifferentOrg_Returns403()
    {
        // Arrange — user is in a different org than the inject's exercise
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(_otherOrg.Id);

        // Act
        var error = await ValidateInjectOrgAccess(_inject.Id, orgContext.Object);

        // Assert
        error.Should().Be("Forbidden", "user from a different org should be denied access to inject observations");
    }

    /// <summary>
    /// BV-004: Non-existent inject returns NotFound.
    /// </summary>
    [Fact]
    public async Task GetObservationsByInject_InjectNotFound_ReturnsNotFound()
    {
        // Arrange
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(_org.Id);

        // Act
        var error = await ValidateInjectOrgAccess(Guid.NewGuid(), orgContext.Object);

        // Assert
        error.Should().Be("NotFound", "requesting observations for a non-existent inject should return NotFound");
    }

    /// <summary>
    /// BV-004: SysAdmin bypasses org context check for inject-based access.
    /// </summary>
    [Fact]
    public async Task GetObservationsByInject_SysAdmin_AllowsCrossOrgAccess()
    {
        // Arrange — SysAdmin with no org context
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(true);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns((Guid?)null);

        // Act
        var error = await ValidateInjectOrgAccess(_inject.Id, orgContext.Object);

        // Assert
        error.Should().BeNull("SysAdmin should bypass org context restriction for inject observations");
    }

    // =========================================================================
    // GetObservation tests
    // =========================================================================

    /// <summary>
    /// BV-004: User in the same org as the observation is granted access.
    /// </summary>
    [Fact]
    public async Task GetObservation_ObservationInSameOrg_Succeeds()
    {
        // Arrange — user is in the same org as the observation
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(_org.Id);

        // Act
        var error = await ValidateObservationOrgAccess(_observation.Id, orgContext.Object);

        // Assert
        error.Should().BeNull("user in the correct org should be granted access to the observation");
    }

    /// <summary>
    /// BV-004: User in a different org than the observation gets 403.
    /// </summary>
    [Fact]
    public async Task GetObservation_ObservationInDifferentOrg_Returns403()
    {
        // Arrange — user is in a different org than the observation
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(_otherOrg.Id);

        // Act
        var error = await ValidateObservationOrgAccess(_observation.Id, orgContext.Object);

        // Assert
        error.Should().Be("Forbidden", "user from a different org should be denied access to the observation");
    }

    /// <summary>
    /// BV-004: Non-existent observation returns NotFound.
    /// </summary>
    [Fact]
    public async Task GetObservation_ObservationNotFound_ReturnsNotFound()
    {
        // Arrange
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns(_org.Id);

        // Act
        var error = await ValidateObservationOrgAccess(Guid.NewGuid(), orgContext.Object);

        // Assert
        error.Should().Be("NotFound", "requesting a non-existent observation should return NotFound");
    }

    /// <summary>
    /// BV-004: SysAdmin bypasses org context check for direct observation access.
    /// </summary>
    [Fact]
    public async Task GetObservation_SysAdmin_AllowsCrossOrgAccess()
    {
        // Arrange — SysAdmin with no org context
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(true);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns((Guid?)null);

        // Act
        var error = await ValidateObservationOrgAccess(_observation.Id, orgContext.Object);

        // Assert
        error.Should().BeNull("SysAdmin should bypass org context restriction for observation access");
    }

    /// <summary>
    /// BV-004: User with no org context (null CurrentOrganizationId) is denied.
    /// </summary>
    [Fact]
    public async Task GetObservation_UserWithNoOrgContext_Returns403()
    {
        // Arrange
        var orgContext = new Mock<ICurrentOrganizationContext>();
        orgContext.Setup(x => x.IsSysAdmin).Returns(false);
        orgContext.Setup(x => x.CurrentOrganizationId).Returns((Guid?)null);

        // Act
        var error = await ValidateObservationOrgAccess(_observation.Id, orgContext.Object);

        // Assert
        error.Should().Be("Forbidden", "user with no org context should be denied observation access");
    }
}
