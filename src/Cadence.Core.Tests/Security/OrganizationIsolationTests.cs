using Cadence.Core.Data;
using Cadence.Core.Data.Interceptors;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Security;

/// <summary>
/// Integration tests for organization data isolation.
/// Verifies that query filters and write validation prevent cross-organization data access.
/// </summary>
public class OrganizationIsolationTests : IDisposable
{
    private readonly Guid _orgAId = Guid.NewGuid();
    private readonly Guid _orgBId = Guid.NewGuid();
    private readonly string _dbName;

    public OrganizationIsolationTests()
    {
        _dbName = $"OrgIsolationTests_{Guid.NewGuid()}";
    }

    public void Dispose()
    {
        // Cleanup is handled by each test with its own context
    }

    /// <summary>
    /// Creates a DbContext configured for a specific user's organization context.
    /// </summary>
    private AppDbContext CreateContextWithOrgContext(
        Guid? currentOrgId,
        bool isSysAdmin = false)
    {
        var orgContextMock = new Mock<ICurrentOrganizationContext>();
        orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(currentOrgId);
        orgContextMock.Setup(x => x.IsSysAdmin).Returns(isSysAdmin);
        orgContextMock.Setup(x => x.CurrentOrgRole).Returns(currentOrgId.HasValue ? OrgRole.OrgAdmin : null);
        orgContextMock.Setup(x => x.HasContext).Returns(true); // Simulate HTTP context exists

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;

        return new AppDbContext(options, orgContextMock.Object);
    }

    /// <summary>
    /// Creates a DbContext without organization context (for seeding data).
    /// </summary>
    private AppDbContext CreateContextWithoutOrgContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Seeds test data with exercises in two different organizations.
    /// </summary>
    private async Task SeedTestDataAsync()
    {
        using var context = CreateContextWithoutOrgContext();

        // Create organizations
        var orgA = new Organization
        {
            Id = _orgAId,
            Name = "Organization A",
            Slug = "org-a",
            Status = OrgStatus.Active,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        var orgB = new Organization
        {
            Id = _orgBId,
            Name = "Organization B",
            Slug = "org-b",
            Status = OrgStatus.Active,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        context.Organizations.AddRange(orgA, orgB);

        // Create exercises in Org A
        for (int i = 1; i <= 3; i++)
        {
            context.Exercises.Add(new Exercise
            {
                Id = Guid.NewGuid(),
                Name = $"Org A Exercise {i}",
                OrganizationId = _orgAId,
                Status = ExerciseStatus.Draft,
                ScheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(i)),
                CreatedBy = Guid.NewGuid(),
                ModifiedBy = Guid.NewGuid()
            });
        }

        // Create exercises in Org B
        for (int i = 1; i <= 2; i++)
        {
            context.Exercises.Add(new Exercise
            {
                Id = Guid.NewGuid(),
                Name = $"Org B Exercise {i}",
                OrganizationId = _orgBId,
                Status = ExerciseStatus.Draft,
                ScheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(i)),
                CreatedBy = Guid.NewGuid(),
                ModifiedBy = Guid.NewGuid()
            });
        }

        await context.SaveChangesAsync();
    }

    // =========================================================================
    // Query Filter Tests
    // =========================================================================

    [Fact]
    public async Task QueryFilter_UserInOrgA_OnlySeesOrgAExercises()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Query as user in Org A
        using var context = CreateContextWithOrgContext(_orgAId);
        var exercises = await context.Exercises.ToListAsync();

        // Assert
        exercises.Should().HaveCount(3);
        exercises.Should().OnlyContain(e => e.OrganizationId == _orgAId);
        exercises.Should().OnlyContain(e => e.Name.StartsWith("Org A"));
    }

    [Fact]
    public async Task QueryFilter_UserInOrgB_OnlySeesOrgBExercises()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Query as user in Org B
        using var context = CreateContextWithOrgContext(_orgBId);
        var exercises = await context.Exercises.ToListAsync();

        // Assert
        exercises.Should().HaveCount(2);
        exercises.Should().OnlyContain(e => e.OrganizationId == _orgBId);
        exercises.Should().OnlyContain(e => e.Name.StartsWith("Org B"));
    }

    [Fact]
    public async Task QueryFilter_SysAdmin_SeesAllExercises()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Query as SysAdmin
        using var context = CreateContextWithOrgContext(currentOrgId: null, isSysAdmin: true);
        var exercises = await context.Exercises.ToListAsync();

        // Assert
        exercises.Should().HaveCount(5);
        exercises.Should().Contain(e => e.OrganizationId == _orgAId);
        exercises.Should().Contain(e => e.OrganizationId == _orgBId);
    }

    [Fact]
    public async Task QueryFilter_UserWithoutOrgContext_SeesNothing()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Query as user with no org context (pending user)
        using var context = CreateContextWithOrgContext(currentOrgId: null, isSysAdmin: false);
        var exercises = await context.Exercises.ToListAsync();

        // Assert - Should see nothing due to org filter
        // Note: Without org context and not SysAdmin, the filter should block all results
        exercises.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryFilter_FindById_ReturnsNullForOtherOrgExercise()
    {
        // Arrange
        await SeedTestDataAsync();

        Guid orgBExerciseId;
        using (var seedContext = CreateContextWithoutOrgContext())
        {
            var orgBExercise = await seedContext.Exercises
                .FirstAsync(e => e.OrganizationId == _orgBId);
            orgBExerciseId = orgBExercise.Id;
        }

        // Act - User in Org A tries to find exercise from Org B
        using var context = CreateContextWithOrgContext(_orgAId);
        var exercise = await context.Exercises.FindAsync(orgBExerciseId);

        // Assert - Should not find it due to query filter
        exercise.Should().BeNull();
    }

    [Fact]
    public async Task QueryFilter_FirstOrDefault_ReturnsNullForOtherOrgExercise()
    {
        // Arrange
        await SeedTestDataAsync();

        Guid orgBExerciseId;
        using (var seedContext = CreateContextWithoutOrgContext())
        {
            var orgBExercise = await seedContext.Exercises
                .FirstAsync(e => e.OrganizationId == _orgBId);
            orgBExerciseId = orgBExercise.Id;
        }

        // Act - User in Org A tries to query exercise from Org B
        using var context = CreateContextWithOrgContext(_orgAId);
        var exercise = await context.Exercises
            .FirstOrDefaultAsync(e => e.Id == orgBExerciseId);

        // Assert - Should not find it due to query filter
        exercise.Should().BeNull();
    }

    [Fact]
    public async Task QueryFilter_IgnoreQueryFilters_BypassesOrgFilter()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Use IgnoreQueryFilters to bypass org filter
        using var context = CreateContextWithOrgContext(_orgAId);
        var exercises = await context.Exercises
            .IgnoreQueryFilters()
            .ToListAsync();

        // Assert - Should see all exercises (including soft-deleted if any)
        exercises.Should().HaveCount(5);
    }

    // =========================================================================
    // Soft Delete + Org Filter Combination Tests
    // =========================================================================

    [Fact]
    public async Task CombinedFilter_SoftDeletedExercise_NotVisibleEvenInSameOrg()
    {
        // Arrange
        await SeedTestDataAsync();

        // Soft delete one exercise in Org A
        using (var context = CreateContextWithoutOrgContext())
        {
            var exercise = await context.Exercises
                .IgnoreQueryFilters()
                .FirstAsync(e => e.OrganizationId == _orgAId);
            exercise.IsDeleted = true;
            exercise.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        // Act - Query as user in Org A
        using var queryContext = CreateContextWithOrgContext(_orgAId);
        var exercises = await queryContext.Exercises.ToListAsync();

        // Assert - Should only see 2 (one was soft deleted)
        exercises.Should().HaveCount(2);
    }

    // =========================================================================
    // Agency Entity Tests (Another IOrganizationScoped entity)
    // =========================================================================

    [Fact]
    public async Task QueryFilter_Agencies_FilteredByOrganization()
    {
        // Arrange - Seed agencies in different orgs
        using (var seedContext = CreateContextWithoutOrgContext())
        {
            await seedContext.Organizations.AddRangeAsync(
                new Organization
                {
                    Id = _orgAId,
                    Name = "Org A",
                    Slug = "org-a",
                    Status = OrgStatus.Active,
                    CreatedBy = Guid.NewGuid(),
                    ModifiedBy = Guid.NewGuid()
                },
                new Organization
                {
                    Id = _orgBId,
                    Name = "Org B",
                    Slug = "org-b",
                    Status = OrgStatus.Active,
                    CreatedBy = Guid.NewGuid(),
                    ModifiedBy = Guid.NewGuid()
                });

            await seedContext.Agencies.AddRangeAsync(
                new Agency
                {
                    Id = Guid.NewGuid(),
                    Name = "Fire Department",
                    OrganizationId = _orgAId,
                    CreatedBy = Guid.NewGuid(),
                    ModifiedBy = Guid.NewGuid()
                },
                new Agency
                {
                    Id = Guid.NewGuid(),
                    Name = "Police Department",
                    OrganizationId = _orgAId,
                    CreatedBy = Guid.NewGuid(),
                    ModifiedBy = Guid.NewGuid()
                },
                new Agency
                {
                    Id = Guid.NewGuid(),
                    Name = "EMS",
                    OrganizationId = _orgBId,
                    CreatedBy = Guid.NewGuid(),
                    ModifiedBy = Guid.NewGuid()
                });

            await seedContext.SaveChangesAsync();
        }

        // Act - Query as user in Org A
        using var context = CreateContextWithOrgContext(_orgAId);
        var agencies = await context.Agencies.ToListAsync();

        // Assert
        agencies.Should().HaveCount(2);
        agencies.Should().OnlyContain(a => a.OrganizationId == _orgAId);
    }

    // =========================================================================
    // Capability Entity Tests (IOrganizationScoped but NOT ISoftDeletable)
    // =========================================================================

    [Fact]
    public async Task QueryFilter_Capabilities_FilteredByOrganization()
    {
        // Arrange - Seed capabilities in different orgs
        using (var seedContext = CreateContextWithoutOrgContext())
        {
            await seedContext.Organizations.AddRangeAsync(
                new Organization
                {
                    Id = _orgAId,
                    Name = "Org A",
                    Slug = "org-a",
                    Status = OrgStatus.Active,
                    CreatedBy = Guid.NewGuid(),
                    ModifiedBy = Guid.NewGuid()
                },
                new Organization
                {
                    Id = _orgBId,
                    Name = "Org B",
                    Slug = "org-b",
                    Status = OrgStatus.Active,
                    CreatedBy = Guid.NewGuid(),
                    ModifiedBy = Guid.NewGuid()
                });

            await seedContext.Capabilities.AddRangeAsync(
                new Capability
                {
                    Id = Guid.NewGuid(),
                    Name = "Mass Care Services",
                    OrganizationId = _orgAId
                },
                new Capability
                {
                    Id = Guid.NewGuid(),
                    Name = "Public Information",
                    OrganizationId = _orgBId
                });

            await seedContext.SaveChangesAsync();
        }

        // Act - Query as user in Org A
        using var context = CreateContextWithOrgContext(_orgAId);
        var capabilities = await context.Capabilities.ToListAsync();

        // Assert
        capabilities.Should().HaveCount(1);
        capabilities.Should().OnlyContain(c => c.OrganizationId == _orgAId);
        capabilities.First().Name.Should().Be("Mass Care Services");
    }
}
