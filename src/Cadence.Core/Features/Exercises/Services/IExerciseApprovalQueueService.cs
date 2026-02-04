using Cadence.Core.Features.Exercises.Models.DTOs;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service interface for exercise approval queue operations (S06: Approval Queue View).
/// </summary>
public interface IExerciseApprovalQueueService
{
    /// <summary>
    /// Gets approval status summary for an exercise.
    /// Counts injects by status (Draft, Submitted, Approved+) to show approval progress.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Approval status with counts and percentage</returns>
    Task<ApprovalStatusDto> GetApprovalStatusAsync(Guid exerciseId, CancellationToken cancellationToken = default);
}
