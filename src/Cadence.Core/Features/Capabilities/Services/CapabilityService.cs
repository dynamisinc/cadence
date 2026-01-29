using Cadence.Core.Data;
using Cadence.Core.Features.Capabilities.Mappers;
using Cadence.Core.Features.Capabilities.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Capabilities.Services;

/// <summary>
/// Service implementation for managing organizational capabilities.
/// </summary>
public class CapabilityService : ICapabilityService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CapabilityService> _logger;

    public CapabilityService(AppDbContext context, ILogger<CapabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CapabilityDto>> GetCapabilitiesAsync(Guid organizationId, bool includeInactive = false)
    {
        var query = _context.Capabilities
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        // Use server-side projection for better performance
        var capabilities = await query
            .OrderBy(c => c.Category ?? "zzz") // Null categories sort last
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CapabilityDto(
                c.Id,
                c.OrganizationId,
                c.Name,
                c.Description,
                c.Category,
                c.SortOrder,
                c.IsActive,
                c.SourceLibrary,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync();

        return capabilities;
    }

    /// <inheritdoc />
    public async Task<CapabilityDto?> GetCapabilityAsync(Guid organizationId, Guid id)
    {
        var capability = await _context.Capabilities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        return capability?.ToDto();
    }

    /// <inheritdoc />
    public async Task<CapabilityDto> CreateCapabilityAsync(Guid organizationId, CreateCapabilityRequest request)
    {
        // Validate organization exists
        var orgExists = await _context.Organizations.AnyAsync(o => o.Id == organizationId);
        if (!orgExists)
        {
            throw new InvalidOperationException($"Organization {organizationId} not found");
        }

        // Check name uniqueness (case-insensitive)
        if (!await IsNameUniqueAsync(organizationId, request.Name))
        {
            throw new InvalidOperationException($"A capability named '{request.Name}' already exists in this organization");
        }

        var capability = request.ToEntity(organizationId);

        _context.Capabilities.Add(capability);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created capability {CapabilityId}: {CapabilityName} for organization {OrganizationId}",
            capability.Id, capability.Name, organizationId);

        return capability.ToDto();
    }

    /// <inheritdoc />
    public async Task<CapabilityDto?> UpdateCapabilityAsync(Guid organizationId, Guid id, UpdateCapabilityRequest request)
    {
        var capability = await _context.Capabilities
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (capability == null)
        {
            return null;
        }

        // Check name uniqueness if name is changing (case-insensitive)
        if (!string.Equals(capability.Name, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            if (!await IsNameUniqueAsync(organizationId, request.Name, excludeId: id))
            {
                throw new InvalidOperationException($"A capability named '{request.Name}' already exists in this organization");
            }
        }

        capability.UpdateFromRequest(request);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated capability {CapabilityId}: {CapabilityName}",
            id, capability.Name);

        return capability.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateCapabilityAsync(Guid organizationId, Guid id)
    {
        var capability = await _context.Capabilities
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (capability == null)
        {
            return false;
        }

        capability.IsActive = false;
        capability.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Deactivated capability {CapabilityId}: {CapabilityName}",
            id, capability.Name);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ReactivateCapabilityAsync(Guid organizationId, Guid id)
    {
        var capability = await _context.Capabilities
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (capability == null)
        {
            return false;
        }

        capability.IsActive = true;
        capability.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Reactivated capability {CapabilityId}: {CapabilityName}",
            id, capability.Name);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsNameUniqueAsync(Guid organizationId, string name, Guid? excludeId = null)
    {
        var trimmedName = name.Trim();

        var query = _context.Capabilities
            .Where(c => c.OrganizationId == organizationId)
            .Where(c => c.Name.ToLower() == trimmedName.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }
}
