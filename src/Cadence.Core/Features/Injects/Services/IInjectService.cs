using Cadence.Core.Features.Injects.Models.DTOs;

namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Service interface for inject conduct operations (firing, skipping, resetting).
/// </summary>
public interface IInjectService
{
    /// <summary>
    /// Fire an inject (deliver to players).
    /// </summary>
    Task<InjectDto> FireInjectAsync(Guid exerciseId, Guid injectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Skip an inject (intentionally not delivered).
    /// </summary>
    Task<InjectDto> SkipInjectAsync(Guid exerciseId, Guid injectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset an inject back to pending status.
    /// </summary>
    Task<InjectDto> ResetInjectAsync(Guid exerciseId, Guid injectId, CancellationToken cancellationToken = default);
}
