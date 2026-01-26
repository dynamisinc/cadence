using Cadence.Core.Features.Assignments.Models.DTOs;

namespace Cadence.Core.Features.Assignments.Services;

/// <summary>
/// Service for managing user exercise assignments.
/// </summary>
public interface IAssignmentService
{
    /// <summary>
    /// Get all exercise assignments for the current user, grouped by status.
    /// Returns Active, Upcoming, and Completed sections.
    /// </summary>
    /// <param name="userId">User ID (ApplicationUser.Id string)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Assignments grouped by status</returns>
    Task<MyAssignmentsResponse> GetMyAssignmentsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Get a single assignment for a user in an exercise.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Assignment or null if not found</returns>
    Task<AssignmentDto?> GetAssignmentAsync(string userId, Guid exerciseId, CancellationToken ct = default);
}
