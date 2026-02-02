using Cadence.Core.Data;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Tests.Data;

public class AppDbContextTests
{
    [Fact]
    public async Task SaveChanges_NewEntity_SetsCreatedAtAndUpdatedAt()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };

        // Act
        var beforeSave = DateTime.UtcNow;
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        var afterSave = DateTime.UtcNow;

        // Assert
        organization.CreatedAt.Should().BeOnOrAfter(beforeSave).And.BeOnOrBefore(afterSave);
        organization.UpdatedAt.Should().BeOnOrAfter(beforeSave).And.BeOnOrBefore(afterSave);
        // CreatedAt and UpdatedAt should be very close (within 1 second) for new entities
        organization.CreatedAt.Should().BeCloseTo(organization.UpdatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SaveChanges_ModifiedEntity_UpdatesOnlyUpdatedAt()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var originalCreatedAt = organization.CreatedAt;

        // Small delay to ensure time difference
        await Task.Delay(10);

        // Act
        var beforeUpdate = DateTime.UtcNow;
        organization.Name = "Updated Organization";
        await context.SaveChangesAsync();
        var afterUpdate = DateTime.UtcNow;

        // Assert
        organization.CreatedAt.Should().Be(originalCreatedAt);
        organization.UpdatedAt.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
        organization.UpdatedAt.Should().BeAfter(organization.CreatedAt);
    }

    [Fact]
    public async Task SoftDelete_QueryFilter_ExcludesDeletedEntities()
    {
        // Arrange
        var context = TestDbContextFactory.Create();

        // Count existing organizations from seed data
        var initialCount = await context.Organizations.CountAsync();

        var activeOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Active Organization",
            IsDeleted = false,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        var deletedOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Organization",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };

        context.Organizations.AddRange(activeOrg, deletedOrg);
        await context.SaveChangesAsync();

        // Act
        var organizations = await context.Organizations.ToListAsync();

        // Assert - should have initial seed data + 1 active org (deleted org filtered out)
        organizations.Should().HaveCount(initialCount + 1);
        organizations.Should().Contain(o => o.Name == "Active Organization");
        organizations.Should().NotContain(o => o.Name == "Deleted Organization");
    }

    [Fact]
    public async Task Exercise_RequiredFields_AreEnforced()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = organization.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };

        // Act
        context.Exercises.Add(exercise);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Exercises.FindAsync(exercise.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Exercise");
        saved.ExerciseType.Should().Be(ExerciseType.TTX);
        saved.Status.Should().Be(ExerciseStatus.Draft);
    }

    [Fact]
    public async Task Inject_RequiredFields_AreEnforced()
    {
        // Arrange
        var context = TestDbContextFactory.Create();

        // Create organization
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(organization);

        // Create exercise
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = organization.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Exercises.Add(exercise);

        // Create MSEL
        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Msels.Add(msel);
        await context.SaveChangesAsync();

        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = 1,
            Title = "Test Inject",
            Description = "Test inject description",
            ScheduledTime = new TimeOnly(9, 0),
            Target = "Test Target",
            InjectType = InjectType.Standard,
            Status = InjectStatus.Pending,
            Sequence = 1,
            MselId = msel.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };

        // Act
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Injects.FindAsync(inject.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Test Inject");
        saved.Target.Should().Be("Test Target");
        saved.Status.Should().Be(InjectStatus.Pending);
    }
}
