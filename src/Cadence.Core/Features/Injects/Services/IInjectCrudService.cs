using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Service interface for inject CRUD operations.
/// Encapsulates the query logic and database interactions for inject
/// create, read, update, and delete scenarios.
/// </summary>
public interface IInjectCrudService
{
    /// <summary>
    /// Returns all injects for the active MSEL of the given exercise.
    /// Supports optional filtering by status and by the submitting user.
    /// Returns an empty list if no active MSEL exists.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="status">Optional inject status filter.</param>
    /// <param name="currentUserId">Current user's ID (used when <paramref name="mySubmissionsOnly"/> is true).</param>
    /// <param name="mySubmissionsOnly">When true, returns only injects submitted or created by <paramref name="currentUserId"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<InjectDto>> GetInjectsAsync(
        Guid exerciseId,
        InjectStatus? status,
        string? currentUserId,
        bool mySubmissionsOnly,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a single inject by ID, scoped to the exercise's active MSEL.
    /// Returns null when the inject is not found.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="injectId">The inject ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<InjectDto?> GetInjectAsync(
        Guid exerciseId,
        Guid injectId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the ordered status-change history (audit trail) for an inject.
    /// Throws <see cref="KeyNotFoundException"/> if the inject does not belong to this exercise.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="injectId">The inject ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<InjectStatusHistoryDto>> GetInjectHistoryAsync(
        Guid exerciseId,
        Guid injectId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new inject for an exercise.
    /// Auto-creates an MSEL if the exercise has no active MSEL.
    /// Validates the request using FluentValidation.
    /// Throws <see cref="KeyNotFoundException"/> if the exercise is not found.
    /// Throws <see cref="FluentValidation.ValidationException"/> for invalid input.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="request">The create request data.</param>
    /// <param name="userId">The ID of the creating user.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<InjectDto> CreateInjectAsync(
        Guid exerciseId,
        CreateInjectRequest request,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing inject.
    /// Applies status-based restrictions (Released injects can only update ControllerNotes).
    /// When approval workflow is enabled, editing an Approved or Submitted inject reverts it to Draft.
    /// Throws <see cref="KeyNotFoundException"/> if the exercise or inject is not found.
    /// Throws <see cref="InvalidOperationException"/> if the exercise is archived.
    /// Throws <see cref="FluentValidation.ValidationException"/> for invalid input.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="injectId">The inject ID.</param>
    /// <param name="request">The update request data.</param>
    /// <param name="userId">The ID of the modifying user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple of the updated DTO and a flag indicating whether approval status was reverted to Draft.</returns>
    Task<(InjectDto dto, bool statusReverted)> UpdateInjectAsync(
        Guid exerciseId,
        Guid injectId,
        UpdateInjectRequest request,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes an inject.
    /// Throws <see cref="KeyNotFoundException"/> if the exercise or inject is not found.
    /// Throws <see cref="InvalidOperationException"/> if the exercise is archived.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="injectId">The inject ID.</param>
    /// <param name="userId">The ID of the deleting user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the inject was deleted.</returns>
    Task<bool> DeleteInjectAsync(
        Guid exerciseId,
        Guid injectId,
        string userId,
        CancellationToken ct = default);
}
