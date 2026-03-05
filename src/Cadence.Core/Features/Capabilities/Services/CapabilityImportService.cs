using Cadence.Core.Data;
using Cadence.Core.Features.Capabilities.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Capabilities.Services;

/// <summary>
/// Service implementation for importing predefined capability libraries.
/// </summary>
public class CapabilityImportService : ICapabilityImportService
{
    private readonly AppDbContext _context;
    private readonly IPredefinedLibraryProvider _libraryProvider;
    private readonly ILogger<CapabilityImportService> _logger;

    public CapabilityImportService(
        AppDbContext context,
        IPredefinedLibraryProvider libraryProvider,
        ILogger<CapabilityImportService> logger)
    {
        _context = context;
        _libraryProvider = libraryProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ImportLibraryResult> ImportLibraryAsync(
        Guid organizationId,
        string libraryId,
        CancellationToken cancellationToken = default)
    {
        // Validate organization exists
        var orgExists = await _context.Organizations
            .AnyAsync(o => o.Id == organizationId, cancellationToken);

        if (!orgExists)
        {
            throw new InvalidOperationException($"Organization {organizationId} not found");
        }

        // Get library from provider
        var library = _libraryProvider.GetLibrary(libraryId);
        if (library == null)
        {
            throw new InvalidOperationException(
                $"Library '{libraryId}' not found. Available libraries: FEMA, NATO, NIST, ISO");
        }

        // Get existing capability names for this organization (case-insensitive)
        var existingNames = await _context.Capabilities
            .Where(c => c.OrganizationId == organizationId)
            .Select(c => c.Name.ToLowerInvariant())
            .ToHashSetAsync(cancellationToken);

        var importedNames = new List<string>();
        var skippedCount = 0;

        // Import capabilities that don't already exist
        foreach (var predefinedCap in library.Capabilities)
        {
            var nameLower = predefinedCap.Name.ToLowerInvariant();

            if (existingNames.Contains(nameLower))
            {
                skippedCount++;
                _logger.LogDebug(
                    "Skipping duplicate capability '{CapabilityName}' for organization {OrganizationId}",
                    predefinedCap.Name, organizationId);
                continue;
            }

            var capability = new Capability
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = predefinedCap.Name,
                Description = predefinedCap.Description,
                Category = predefinedCap.Category,
                SortOrder = 0,
                IsActive = true,
                SourceLibrary = library.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Capabilities.Add(capability);
            importedNames.Add(predefinedCap.Name);

            // Add to existing names to prevent duplicates within same import batch
            existingNames.Add(nameLower);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Imported {ImportedCount} capabilities from library '{LibraryId}' into organization {OrganizationId}. " +
            "Skipped {SkippedCount} duplicates.",
            importedNames.Count, library.Id, organizationId, skippedCount);

        return new ImportLibraryResult(
            TotalInLibrary: library.Capabilities.Count,
            Imported: importedNames.Count,
            SkippedDuplicates: skippedCount,
            ImportedNames: importedNames
        );
    }
}
