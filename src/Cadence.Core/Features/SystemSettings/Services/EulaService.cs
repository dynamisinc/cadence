using Cadence.Core.Data;
using Cadence.Core.Features.SystemSettings.Models.DTOs;
using Cadence.Core.Features.SystemSettings.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.SystemSettings.Services;

public class EulaService : IEulaService
{
    private readonly AppDbContext _context;

    public EulaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EulaStatusDto> GetStatusAsync(string userId)
    {
        var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();

        // No EULA configured
        if (settings == null || string.IsNullOrEmpty(settings.EulaVersion) || string.IsNullOrEmpty(settings.EulaContent))
        {
            return new EulaStatusDto { Required = false };
        }

        // Check if user has already accepted the current version
        var hasAccepted = await _context.EulaAcceptances
            .AnyAsync(a => a.UserId == userId && a.EulaVersion == settings.EulaVersion);

        if (hasAccepted)
        {
            return new EulaStatusDto
            {
                Required = false,
                Version = settings.EulaVersion,
            };
        }

        return new EulaStatusDto
        {
            Required = true,
            Version = settings.EulaVersion,
            Content = settings.EulaContent,
        };
    }

    public async Task AcceptAsync(string userId, string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("EULA version is required.", nameof(version));

        // Validate version matches the currently configured EULA
        var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.EulaVersion))
            throw new InvalidOperationException("No EULA is currently configured.");

        if (!string.Equals(version, settings.EulaVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("The submitted EULA version does not match the current version.");

        // Check if already accepted (idempotent)
        var existing = await _context.EulaAcceptances
            .AnyAsync(a => a.UserId == userId && a.EulaVersion == version);

        if (existing) return;

        _context.EulaAcceptances.Add(new EulaAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EulaVersion = version,
            AcceptedAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();
    }
}
