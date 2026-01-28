using Cadence.Core.Data;
using Cadence.Core.Features.Capabilities.Models.DTOs;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for managing exercise target capabilities (S04).
/// </summary>
public class ExerciseCapabilityService : IExerciseCapabilityService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExerciseCapabilityService> _logger;

    public ExerciseCapabilityService(
        AppDbContext context,
        ILogger<ExerciseCapabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CapabilityDto>> GetTargetCapabilitiesAsync(
        Guid exerciseId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting target capabilities for exercise {ExerciseId}", exerciseId);

        var capabilities = await _context.ExerciseTargetCapabilities
            .AsNoTracking()
            .Where(etc => etc.ExerciseId == exerciseId)
            .Where(etc => etc.Capability.IsActive) // Only return active capabilities
            .OrderBy(etc => etc.Capability.Category ?? string.Empty)
            .ThenBy(etc => etc.Capability.Name)
            .Select(etc => new CapabilityDto(
                etc.Capability.Id,
                etc.Capability.OrganizationId,
                etc.Capability.Name,
                etc.Capability.Description,
                etc.Capability.Category,
                etc.Capability.SortOrder,
                etc.Capability.IsActive,
                etc.Capability.SourceLibrary,
                etc.Capability.CreatedAt,
                etc.Capability.UpdatedAt
            ))
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} target capabilities for exercise {ExerciseId}",
            capabilities.Count, exerciseId);

        return capabilities;
    }

    /// <inheritdoc />
    public async Task SetTargetCapabilitiesAsync(
        Guid exerciseId,
        List<Guid> capabilityIds,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Setting target capabilities for exercise {ExerciseId}: {Count} capabilities",
            exerciseId, capabilityIds.Count);

        // Remove duplicates
        var distinctIds = capabilityIds.Distinct().ToList();

        // Remove existing links
        var existingLinks = await _context.ExerciseTargetCapabilities
            .Where(etc => etc.ExerciseId == exerciseId)
            .ToListAsync(ct);

        if (existingLinks.Any())
        {
            _context.ExerciseTargetCapabilities.RemoveRange(existingLinks);
            _logger.LogDebug("Removed {Count} existing capability links for exercise {ExerciseId}",
                existingLinks.Count, exerciseId);
        }

        // Add new links
        if (distinctIds.Any())
        {
            var newLinks = distinctIds.Select(capabilityId => new ExerciseTargetCapability
            {
                ExerciseId = exerciseId,
                CapabilityId = capabilityId
            }).ToList();

            await _context.ExerciseTargetCapabilities.AddRangeAsync(newLinks, ct);
            _logger.LogDebug("Added {Count} new capability links for exercise {ExerciseId}",
                newLinks.Count, exerciseId);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Successfully updated target capabilities for exercise {ExerciseId}",
            exerciseId);
    }

    /// <inheritdoc />
    public async Task<ExerciseCapabilitySummaryDto> GetCapabilitySummaryAsync(
        Guid exerciseId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting capability summary for exercise {ExerciseId}", exerciseId);

        // Get count of target capabilities
        var targetCount = await _context.ExerciseTargetCapabilities
            .AsNoTracking()
            .CountAsync(etc => etc.ExerciseId == exerciseId, ct);

        if (targetCount == 0)
        {
            _logger.LogDebug("No target capabilities found for exercise {ExerciseId}", exerciseId);
            return new ExerciseCapabilitySummaryDto(0, 0, null);
        }

        // Get count of target capabilities that have been evaluated
        // A capability is "evaluated" if it appears in at least one observation
        var evaluatedCount = await _context.ExerciseTargetCapabilities
            .AsNoTracking()
            .Where(etc => etc.ExerciseId == exerciseId)
            .Where(etc => _context.ObservationCapabilities
                .Any(oc => oc.CapabilityId == etc.CapabilityId &&
                           oc.Observation.ExerciseId == exerciseId))
            .Select(etc => etc.CapabilityId)
            .Distinct()
            .CountAsync(ct);

        // Calculate coverage percentage
        var coveragePercentage = targetCount > 0
            ? Math.Round((decimal)evaluatedCount / targetCount * 100, 2)
            : 0m;

        _logger.LogInformation(
            "Capability summary for exercise {ExerciseId}: {TargetCount} targets, {EvaluatedCount} evaluated ({CoveragePercentage}% coverage)",
            exerciseId, targetCount, evaluatedCount, coveragePercentage);

        return new ExerciseCapabilitySummaryDto(
            targetCount,
            evaluatedCount,
            coveragePercentage
        );
    }
}
