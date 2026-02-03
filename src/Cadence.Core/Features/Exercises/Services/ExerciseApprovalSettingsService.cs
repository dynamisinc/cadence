using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for managing exercise-level inject approval settings.
/// Part of S02: Exercise Approval Configuration.
/// </summary>
public class ExerciseApprovalSettingsService : IExerciseApprovalSettingsService
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly ILogger<ExerciseApprovalSettingsService> _logger;

    public ExerciseApprovalSettingsService(
        AppDbContext context,
        ICurrentOrganizationContext orgContext,
        ILogger<ExerciseApprovalSettingsService> logger)
    {
        _context = context;
        _orgContext = orgContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApprovalSettingsDto> GetApprovalSettingsAsync(
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises
            .Include(e => e.Organization)
            .FirstOrDefaultAsync(e => e.Id == exerciseId, ct);

        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise with ID {exerciseId} not found");
        }

        return new ApprovalSettingsDto(
            exercise.RequireInjectApproval,
            exercise.ApprovalPolicyOverridden,
            exercise.ApprovalOverrideReason,
            exercise.ApprovalOverriddenById,
            exercise.ApprovalOverriddenAt,
            exercise.Organization.InjectApprovalPolicy);
    }

    /// <inheritdoc />
    public async Task<ApprovalSettingsDto> UpdateApprovalSettingsAsync(
        Guid exerciseId,
        UpdateApprovalSettingsRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises
            .Include(e => e.Organization)
            .FirstOrDefaultAsync(e => e.Id == exerciseId, ct);

        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise with ID {exerciseId} not found");
        }

        var orgPolicy = exercise.Organization.InjectApprovalPolicy;
        var isAdmin = _orgContext.IsSysAdmin;

        // Validate against organization policy
        ValidateApprovalChange(
            orgPolicy,
            request.RequireInjectApproval,
            isAdmin,
            request.IsOverride);

        // Handle override scenario (Required policy + Admin disabling approval)
        if (orgPolicy == ApprovalPolicy.Required &&
            !request.RequireInjectApproval &&
            isAdmin &&
            request.IsOverride)
        {
            exercise.ApprovalPolicyOverridden = true;
            exercise.ApprovalOverrideReason = request.OverrideReason;
            exercise.ApprovalOverriddenById = userId;
            exercise.ApprovalOverriddenAt = DateTime.UtcNow;

            _logger.LogWarning(
                "Admin {UserId} overrode approval policy for exercise {ExerciseId}. Reason: {Reason}",
                userId, exerciseId, request.OverrideReason ?? "(none)");
        }
        // Handle restoring organization policy
        else if (request.RequireInjectApproval && exercise.ApprovalPolicyOverridden)
        {
            exercise.ApprovalPolicyOverridden = false;
            exercise.ApprovalOverrideReason = null;
            exercise.ApprovalOverriddenById = null;
            exercise.ApprovalOverriddenAt = null;

            _logger.LogInformation(
                "User {UserId} restored organization approval policy for exercise {ExerciseId}",
                userId, exerciseId);
        }

        // Handle disabling approval with pending (Submitted) injects
        if (exercise.RequireInjectApproval && !request.RequireInjectApproval)
        {
            await AutoApproveSubmittedInjectsAsync(exerciseId, ct);
        }

        // Update the primary setting
        exercise.RequireInjectApproval = request.RequireInjectApproval;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated approval settings for exercise {ExerciseId}: RequireApproval={RequireApproval}, Override={Override}",
            exerciseId, request.RequireInjectApproval, exercise.ApprovalPolicyOverridden);

        return new ApprovalSettingsDto(
            exercise.RequireInjectApproval,
            exercise.ApprovalPolicyOverridden,
            exercise.ApprovalOverrideReason,
            exercise.ApprovalOverriddenById,
            exercise.ApprovalOverriddenAt,
            orgPolicy);
    }

    /// <summary>
    /// Validates that the requested approval change is allowed given the organization policy.
    /// </summary>
    private static void ValidateApprovalChange(
        ApprovalPolicy orgPolicy,
        bool requestedApprovalState,
        bool isAdmin,
        bool isOverride)
    {
        // Policy: Disabled - approval cannot be enabled
        if (orgPolicy == ApprovalPolicy.Disabled && requestedApprovalState)
        {
            throw new InvalidOperationException(
                "Cannot enable inject approval - organization policy has disabled approval workflow");
        }

        // Policy: Required - non-admins cannot disable approval
        if (orgPolicy == ApprovalPolicy.Required && !requestedApprovalState && !isAdmin)
        {
            throw new InvalidOperationException(
                "Cannot disable inject approval - organization policy requires approval for all exercises. " +
                "Contact an administrator to override.");
        }

        // Policy: Required - admin must explicitly set IsOverride flag
        if (orgPolicy == ApprovalPolicy.Required && !requestedApprovalState && isAdmin && !isOverride)
        {
            throw new InvalidOperationException(
                "To disable inject approval when organization policy requires it, IsOverride must be set to true");
        }
    }

    /// <summary>
    /// Auto-approves all injects in Submitted status when approval workflow is disabled.
    /// This prevents injects from being stuck in Submitted status.
    /// </summary>
    private async Task AutoApproveSubmittedInjectsAsync(Guid exerciseId, CancellationToken ct)
    {
        var submittedInjects = await _context.Injects
            .Where(i => i.Msel.ExerciseId == exerciseId && i.Status == InjectStatus.Submitted)
            .ToListAsync(ct);

        if (submittedInjects.Count == 0)
        {
            return;
        }

        foreach (var inject in submittedInjects)
        {
            inject.Status = InjectStatus.Approved;
            inject.ApprovedAt = DateTime.UtcNow;
            // ApprovedByUserId left null to indicate auto-approval
        }

        _logger.LogInformation(
            "Auto-approved {Count} submitted injects for exercise {ExerciseId} when disabling approval workflow",
            submittedInjects.Count, exerciseId);
    }
}
