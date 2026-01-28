using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Features.Exercises;

/// <summary>
/// Tests for ExerciseCapabilityService.
/// Covers S04 - Exercise Target Capabilities management.
/// </summary>
public class ExerciseCapabilityServiceTests
{
    private readonly AppDbContext _context;
    private readonly ExerciseCapabilityService _service;
    private readonly Guid _testOrganizationId = Guid.NewGuid();

    public ExerciseCapabilityServiceTests()
    {
        _context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<ExerciseCapabilityService>>();
        _service = new ExerciseCapabilityService(_context, logger.Object);
    }

    /// <summary>
    /// Helper to create a test exercise.
    /// </summary>
    private async Task<Exercise> CreateExerciseAsync(string name = "Test Exercise")
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = name,
            OrganizationId = _testOrganizationId,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            ExerciseType = ExerciseType.TTX
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();
        return exercise;
    }

    /// <summary>
    /// Helper to create a test capability.
    /// </summary>
    private async Task<Capability> CreateCapabilityAsync(
        string name,
        bool isActive = true,
        string? category = null)
    {
        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = _testOrganizationId,
            Name = name,
            IsActive = isActive,
            Category = category,
            SortOrder = 0
        };

        _context.Capabilities.Add(capability);
        await _context.SaveChangesAsync();
        return capability;
    }

    /// <summary>
    /// Helper to link a capability to an exercise.
    /// </summary>
    private async Task LinkCapabilityAsync(Guid exerciseId, Guid capabilityId)
    {
        var link = new ExerciseTargetCapability
        {
            ExerciseId = exerciseId,
            CapabilityId = capabilityId
        };

        _context.ExerciseTargetCapabilities.Add(link);
        await _context.SaveChangesAsync();
    }

    // =========================================================================
    // GetTargetCapabilitiesAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetTargetCapabilitiesAsync_NoLinkedCapabilities_ReturnsEmpty()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();

        // Act
        var result = await _service.GetTargetCapabilitiesAsync(exercise.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTargetCapabilitiesAsync_WithLinkedCapabilities_ReturnsCapabilities()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var capability1 = await CreateCapabilityAsync("Planning");
        var capability2 = await CreateCapabilityAsync("Operational Coordination");

        await LinkCapabilityAsync(exercise.Id, capability1.Id);
        await LinkCapabilityAsync(exercise.Id, capability2.Id);

        // Act
        var result = await _service.GetTargetCapabilitiesAsync(exercise.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, c => c.Name == "Planning");
        Assert.Contains(result, c => c.Name == "Operational Coordination");
    }

    [Fact]
    public async Task GetTargetCapabilitiesAsync_ExcludesInactiveCapabilities()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var activeCapability = await CreateCapabilityAsync("Active Capability", isActive: true);
        var inactiveCapability = await CreateCapabilityAsync("Inactive Capability", isActive: false);

        await LinkCapabilityAsync(exercise.Id, activeCapability.Id);
        await LinkCapabilityAsync(exercise.Id, inactiveCapability.Id);

        // Act
        var result = await _service.GetTargetCapabilitiesAsync(exercise.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(result, c => c.Name == "Active Capability");
        Assert.DoesNotContain(result, c => c.Name == "Inactive Capability");
    }

    [Fact]
    public async Task GetTargetCapabilitiesAsync_OrdersByCategory_ThenName()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var cap1 = await CreateCapabilityAsync("Zulu", category: "Response");
        var cap2 = await CreateCapabilityAsync("Alpha", category: "Response");
        var cap3 = await CreateCapabilityAsync("Beta", category: "Prevention");

        await LinkCapabilityAsync(exercise.Id, cap1.Id);
        await LinkCapabilityAsync(exercise.Id, cap2.Id);
        await LinkCapabilityAsync(exercise.Id, cap3.Id);

        // Act
        var result = await _service.GetTargetCapabilitiesAsync(exercise.Id);

        // Assert
        var list = result.ToList();
        Assert.Equal(3, list.Count);
        // Should be ordered by category, then by name
        Assert.Equal("Beta", list[0].Name);     // Prevention, Beta
        Assert.Equal("Alpha", list[1].Name);    // Response, Alpha
        Assert.Equal("Zulu", list[2].Name);     // Response, Zulu
    }

    // =========================================================================
    // SetTargetCapabilitiesAsync Tests
    // =========================================================================

    [Fact]
    public async Task SetTargetCapabilitiesAsync_CreatesNewLinks()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var capability1 = await CreateCapabilityAsync("Planning");
        var capability2 = await CreateCapabilityAsync("Communications");
        var capabilityIds = new List<Guid> { capability1.Id, capability2.Id };

        // Act
        await _service.SetTargetCapabilitiesAsync(exercise.Id, capabilityIds);

        // Assert
        var links = _context.ExerciseTargetCapabilities
            .Where(etc => etc.ExerciseId == exercise.Id)
            .ToList();
        Assert.Equal(2, links.Count);
        Assert.Contains(links, l => l.CapabilityId == capability1.Id);
        Assert.Contains(links, l => l.CapabilityId == capability2.Id);
    }

    [Fact]
    public async Task SetTargetCapabilitiesAsync_ReplacesExistingLinks()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var oldCapability = await CreateCapabilityAsync("Old Capability");
        var newCapability = await CreateCapabilityAsync("New Capability");

        // Link old capability
        await LinkCapabilityAsync(exercise.Id, oldCapability.Id);

        // Act - replace with new capability
        await _service.SetTargetCapabilitiesAsync(exercise.Id, new List<Guid> { newCapability.Id });

        // Assert
        var links = _context.ExerciseTargetCapabilities
            .Where(etc => etc.ExerciseId == exercise.Id)
            .ToList();
        Assert.Single(links);
        Assert.Equal(newCapability.Id, links[0].CapabilityId);
    }

    [Fact]
    public async Task SetTargetCapabilitiesAsync_EmptyList_ClearsAllLinks()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var capability1 = await CreateCapabilityAsync("Capability 1");
        var capability2 = await CreateCapabilityAsync("Capability 2");

        await LinkCapabilityAsync(exercise.Id, capability1.Id);
        await LinkCapabilityAsync(exercise.Id, capability2.Id);

        // Act - clear all by passing empty list
        await _service.SetTargetCapabilitiesAsync(exercise.Id, new List<Guid>());

        // Assert
        var links = _context.ExerciseTargetCapabilities
            .Where(etc => etc.ExerciseId == exercise.Id)
            .ToList();
        Assert.Empty(links);
    }

    [Fact]
    public async Task SetTargetCapabilitiesAsync_IgnoresDuplicateIds()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var capability = await CreateCapabilityAsync("Planning");
        var capabilityIds = new List<Guid> { capability.Id, capability.Id }; // Duplicate

        // Act
        await _service.SetTargetCapabilitiesAsync(exercise.Id, capabilityIds);

        // Assert
        var links = _context.ExerciseTargetCapabilities
            .Where(etc => etc.ExerciseId == exercise.Id)
            .ToList();
        Assert.Single(links); // Should only create one link
    }

    // =========================================================================
    // GetCapabilitySummaryAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetCapabilitySummaryAsync_NoTargetCapabilities_ReturnsZeros()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();

        // Act
        var result = await _service.GetCapabilitySummaryAsync(exercise.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TargetCount);
        Assert.Equal(0, result.EvaluatedCount);
        Assert.Null(result.CoveragePercentage);
    }

    [Fact]
    public async Task GetCapabilitySummaryAsync_WithTargets_NoObservations_ReturnsZeroCoverage()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var capability1 = await CreateCapabilityAsync("Planning");
        var capability2 = await CreateCapabilityAsync("Communications");

        await LinkCapabilityAsync(exercise.Id, capability1.Id);
        await LinkCapabilityAsync(exercise.Id, capability2.Id);

        // Act
        var result = await _service.GetCapabilitySummaryAsync(exercise.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TargetCount);
        Assert.Equal(0, result.EvaluatedCount);
        Assert.Equal(0m, result.CoveragePercentage);
    }

    [Fact]
    public async Task GetCapabilitySummaryAsync_WithObservations_CalculatesCoverage()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var capability1 = await CreateCapabilityAsync("Planning");
        var capability2 = await CreateCapabilityAsync("Communications");
        var capability3 = await CreateCapabilityAsync("Logistics");

        await LinkCapabilityAsync(exercise.Id, capability1.Id);
        await LinkCapabilityAsync(exercise.Id, capability2.Id);
        await LinkCapabilityAsync(exercise.Id, capability3.Id);

        // Create observations for two capabilities
        var observation1 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Test observation 1",
            ObservedAt = DateTime.UtcNow
        };
        _context.Observations.Add(observation1);
        await _context.SaveChangesAsync();

        var observation2 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Test observation 2",
            ObservedAt = DateTime.UtcNow
        };
        _context.Observations.Add(observation2);
        await _context.SaveChangesAsync();

        // Link capabilities to observations
        _context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation1.Id,
            CapabilityId = capability1.Id
        });
        _context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation2.Id,
            CapabilityId = capability2.Id
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCapabilitySummaryAsync(exercise.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TargetCount);
        Assert.Equal(2, result.EvaluatedCount);
        Assert.Equal(66.67m, result.CoveragePercentage); // 2/3 = 66.67%
    }

    [Fact]
    public async Task GetCapabilitySummaryAsync_FullCoverage_Returns100Percent()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var capability1 = await CreateCapabilityAsync("Planning");
        var capability2 = await CreateCapabilityAsync("Communications");

        await LinkCapabilityAsync(exercise.Id, capability1.Id);
        await LinkCapabilityAsync(exercise.Id, capability2.Id);

        // Create observations for both capabilities
        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Test observation",
            ObservedAt = DateTime.UtcNow
        };
        _context.Observations.Add(observation);
        await _context.SaveChangesAsync();

        _context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation.Id,
            CapabilityId = capability1.Id
        });
        _context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation.Id,
            CapabilityId = capability2.Id
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCapabilitySummaryAsync(exercise.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TargetCount);
        Assert.Equal(2, result.EvaluatedCount);
        Assert.Equal(100m, result.CoveragePercentage);
    }

    [Fact]
    public async Task GetCapabilitySummaryAsync_CapabilityEvaluatedMultipleTimes_CountsOnce()
    {
        // Arrange
        var exercise = await CreateExerciseAsync();
        var capability = await CreateCapabilityAsync("Planning");

        await LinkCapabilityAsync(exercise.Id, capability.Id);

        // Create multiple observations for the same capability
        var observation1 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "First observation",
            ObservedAt = DateTime.UtcNow
        };
        _context.Observations.Add(observation1);

        var observation2 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Second observation",
            ObservedAt = DateTime.UtcNow
        };
        _context.Observations.Add(observation2);
        await _context.SaveChangesAsync();

        _context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation1.Id,
            CapabilityId = capability.Id
        });
        _context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation2.Id,
            CapabilityId = capability.Id
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCapabilitySummaryAsync(exercise.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TargetCount);
        Assert.Equal(1, result.EvaluatedCount); // Should count capability only once
        Assert.Equal(100m, result.CoveragePercentage);
    }
}
