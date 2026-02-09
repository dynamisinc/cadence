using Cadence.Core.Features.SystemSettings.Models.DTOs;

namespace Cadence.Core.Features.SystemSettings.Services;

public interface ISystemSettingsService
{
    Task<SystemSettingsDto> GetSettingsAsync();
    Task<SystemSettingsDto> UpdateSettingsAsync(UpdateSystemSettingsRequest request, string updatedBy);
}
