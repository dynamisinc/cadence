using Cadence.Core.Data;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.SystemSettings.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Features.SystemSettings.Services;

public class SystemSettingsService : ISystemSettingsService
{
    private readonly AppDbContext _context;
    private readonly EmailServiceOptions _emailDefaults;

    public SystemSettingsService(AppDbContext context, IOptions<EmailServiceOptions> emailDefaults)
    {
        _context = context;
        _emailDefaults = emailDefaults.Value;
    }

    public async Task<SystemSettingsDto> GetSettingsAsync()
    {
        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        return MapToDto(settings);
    }

    public async Task<SystemSettingsDto> UpdateSettingsAsync(UpdateSystemSettingsRequest request, string updatedBy)
    {
        var settings = await _context.SystemSettings.FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new Models.Entities.SystemSettings
            {
                Id = Guid.NewGuid(),
            };
            _context.SystemSettings.Add(settings);
        }

        // Normalize empty strings to null (means "use default")
        settings.SupportAddress = NullIfEmpty(request.SupportAddress);
        settings.DefaultSenderAddress = NullIfEmpty(request.DefaultSenderAddress);
        settings.DefaultSenderName = NullIfEmpty(request.DefaultSenderName);
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();

        return MapToDto(settings);
    }

    private SystemSettingsDto MapToDto(Models.Entities.SystemSettings? settings)
    {
        return new SystemSettingsDto
        {
            Id = settings?.Id,
            SupportAddress = settings?.SupportAddress,
            DefaultSenderAddress = settings?.DefaultSenderAddress,
            DefaultSenderName = settings?.DefaultSenderName,
            EffectiveSupportAddress = settings?.SupportAddress ?? _emailDefaults.SupportAddress,
            EffectiveDefaultSenderAddress = settings?.DefaultSenderAddress ?? _emailDefaults.DefaultSenderAddress,
            EffectiveDefaultSenderName = settings?.DefaultSenderName ?? _emailDefaults.DefaultSenderName,
            UpdatedAt = settings?.UpdatedAt,
            UpdatedBy = settings?.UpdatedBy,
        };
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
