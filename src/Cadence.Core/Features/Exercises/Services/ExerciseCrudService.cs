using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for core exercise CRUD and settings operations.
/// Handles exercise lookup, settings retrieval, and settings updates.
/// </summary>
public class ExerciseCrudService : IExerciseCrudService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExerciseCrudService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ExerciseCrudService"/>.
    /// </summary>
    public ExerciseCrudService(AppDbContext context, ILogger<ExerciseCrudService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ExerciseDto?> GetExerciseAsync(Guid exerciseId, CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        return exercise?.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> ExerciseExistsAsync(Guid exerciseId, CancellationToken ct = default)
    {
        return await _context.Exercises
            .AsNoTracking()
            .AnyAsync(e => e.Id == exerciseId, ct);
    }

    /// <inheritdoc />
    public async Task<ExerciseSettingsDto?> GetExerciseSettingsAsync(Guid exerciseId, CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);

        if (exercise == null)
            return null;

        return new ExerciseSettingsDto(
            exercise.ClockMultiplier,
            exercise.AutoFireEnabled,
            exercise.ConfirmFireInject,
            exercise.ConfirmSkipInject,
            exercise.ConfirmClockControl,
            exercise.MaxDuration);
    }

    /// <inheritdoc />
    public async Task<ExerciseSettingsDto?> UpdateExerciseSettingsAsync(
        Guid exerciseId,
        UpdateExerciseSettingsRequest request,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);

        if (exercise == null)
            return null;

        // Validate clock multiplier change
        if (request.ClockMultiplier.HasValue)
        {
            // Validate range
            if (request.ClockMultiplier < 0.5m || request.ClockMultiplier > 20.0m)
                throw new ArgumentException("Clock multiplier must be between 0.5 and 20.");

            // Can only change when paused or draft
            if (exercise.ClockState == ExerciseClockState.Running)
                throw new InvalidOperationException(
                    "Cannot change clock multiplier while clock is running. Pause the exercise first.");

            exercise.ClockMultiplier = request.ClockMultiplier.Value;
            exercise.TimeScale = request.ClockMultiplier.Value;
        }

        // Update boolean settings (can be changed anytime)
        if (request.AutoFireEnabled.HasValue)
            exercise.AutoFireEnabled = request.AutoFireEnabled.Value;

        if (request.ConfirmFireInject.HasValue)
            exercise.ConfirmFireInject = request.ConfirmFireInject.Value;

        if (request.ConfirmSkipInject.HasValue)
            exercise.ConfirmSkipInject = request.ConfirmSkipInject.Value;

        if (request.ConfirmClockControl.HasValue)
            exercise.ConfirmClockControl = request.ConfirmClockControl.Value;

        // Validate and update max duration
        if (request.MaxDuration.HasValue)
        {
            if (request.MaxDuration.Value <= TimeSpan.Zero)
                throw new ArgumentException("Max duration must be greater than zero.");

            if (request.MaxDuration.Value > TimeSpan.FromDays(14))
                throw new ArgumentException("Max duration cannot exceed 336 hours (2 weeks).");

            exercise.MaxDuration = request.MaxDuration.Value;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated settings for exercise {ExerciseId}: ClockMultiplier={ClockMultiplier}, AutoFire={AutoFire}",
            exerciseId, exercise.ClockMultiplier, exercise.AutoFireEnabled);

        return new ExerciseSettingsDto(
            exercise.ClockMultiplier,
            exercise.AutoFireEnabled,
            exercise.ConfirmFireInject,
            exercise.ConfirmSkipInject,
            exercise.ConfirmClockControl,
            exercise.MaxDuration);
    }
}
