using System.Net.Mail;
using System.Security.Cryptography;
using Cadence.Core.Data;
using Cadence.Core.Exceptions;
using Cadence.Core.Features.Authentication.Models;
using Cadence.Core.Features.BulkParticipantImport.Models.Entities;
using Cadence.Core.Features.Email.Models;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Features.Organizations.Services;

/// <summary>
/// Manages organization invitation lifecycle: create, resend, cancel, validate, accept.
/// </summary>
public class OrganizationInvitationService : IOrganizationInvitationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrganizationInvitationService> _logger;
    private readonly IEmailService _emailService;
    private readonly AuthenticationOptions _authOptions;
    private const int InvitationExpirationDays = 7;
    private const int InviteCodeLength = 8;

    public OrganizationInvitationService(
        AppDbContext context,
        ILogger<OrganizationInvitationService> logger,
        IEmailService emailService,
        IOptions<AuthenticationOptions> authOptions)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _authOptions = authOptions.Value;
    }

    public async Task<InvitationDto> CreateInvitationAsync(
        Guid organizationId,
        CreateInvitationRequest request,
        string invitedByUserId)
    {
        // Validate email format before persisting
        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
        {
            throw new BusinessRuleException("Invalid email address format");
        }

        // Validate organization exists
        var org = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId && !o.IsDeleted)
            ?? throw new NotFoundException("Organization not found");

        // Check if email is already a member
        var existingMembership = await _context.OrganizationMemberships
            .Include(m => m.User)
            .FirstOrDefaultAsync(m =>
                m.OrganizationId == organizationId &&
                m.User != null &&
                m.User.Email == request.Email &&
                m.Status == MembershipStatus.Active);

        if (existingMembership != null)
        {
            throw new ConflictException("This person is already a member of the organization");
        }

        // Check for existing pending invitation
        var existingInvite = await _context.OrganizationInvites
            .FirstOrDefaultAsync(i =>
                i.OrganizationId == organizationId &&
                i.Email == request.Email &&
                !i.IsDeleted &&
                i.UsedAt == null &&
                i.ExpiresAt > DateTime.UtcNow);

        if (existingInvite != null)
        {
            throw new ConflictException("A pending invitation already exists for this email. Use resend to send it again.");
        }

        // Load inviter user for email
        var inviter = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == invitedByUserId);

        var invite = new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Email = request.Email,
            Code = GenerateInviteCode(),
            Role = request.Role,
            ExpiresAt = DateTime.UtcNow.AddDays(InvitationExpirationDays),
            CreatedByUserId = invitedByUserId,
            MaxUses = 1,
            UseCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrganizationInvites.Add(invite);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Invitation created for {Email} to organization {OrgId} by {InvitedBy}",
            request.Email, organizationId, invitedByUserId);

        // Send invitation email (don't fail the operation if email fails)
        var (emailSent, emailError) = await SendInvitationEmailAsync(invite, org.Name, inviter?.DisplayName ?? "A team member");

        var dto = await MapToDto(invite);
        return dto with { EmailSent = emailSent, EmailError = emailError };
    }

    public async Task<InvitationDto> ResendInvitationAsync(Guid invitationId, string requestedByUserId)
    {
        var invite = await _context.OrganizationInvites
            .Include(i => i.CreatedByUser)
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Id == invitationId && !i.IsDeleted)
            ?? throw new NotFoundException("Invitation not found");

        // Cannot resend accepted invitations
        if (invite.UsedAt != null)
        {
            throw new BusinessRuleException("Invitation has already been accepted");
        }

        // Refresh expiration and generate new code
        invite.ExpiresAt = DateTime.UtcNow.AddDays(InvitationExpirationDays);
        invite.Code = GenerateInviteCode();
        invite.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Invitation {InviteId} resent to {Email} by {UserId}",
            invitationId, invite.Email, requestedByUserId);

        // Send invitation email (don't fail the operation if email fails)
        var inviterName = invite.CreatedByUser?.DisplayName ?? "A team member";
        var orgName = invite.Organization?.Name ?? "the organization";
        var (emailSent, emailError) = await SendInvitationEmailAsync(invite, orgName, inviterName);

        var dto = await MapToDto(invite);
        return dto with { EmailSent = emailSent, EmailError = emailError };
    }

    public async Task CancelInvitationAsync(Guid invitationId, string requestedByUserId)
    {
        var invite = await _context.OrganizationInvites
            .FirstOrDefaultAsync(i => i.Id == invitationId && !i.IsDeleted)
            ?? throw new NotFoundException("Invitation not found");

        if (invite.UsedAt != null)
        {
            throw new BusinessRuleException("Cannot cancel an accepted invitation");
        }

        // Soft-delete the invitation (acts as cancellation)
        invite.IsDeleted = true;
        invite.DeletedAt = DateTime.UtcNow;
        invite.UpdatedAt = DateTime.UtcNow;

        // Cancel any pending exercise assignments linked to this invitation
        var pendingAssignments = await _context.PendingExerciseAssignments
            .Where(pa => pa.OrganizationInviteId == invitationId && pa.Status == PendingAssignmentStatus.Pending)
            .ToListAsync();

        foreach (var assignment in pendingAssignments)
        {
            assignment.Status = PendingAssignmentStatus.Cancelled;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Invitation {InviteId} cancelled by {UserId} with {PendingCount} pending assignments also cancelled",
            invitationId, requestedByUserId, pendingAssignments.Count);
    }

    public async Task<IEnumerable<InvitationDto>> GetInvitationsAsync(
        Guid organizationId,
        string? statusFilter = null)
    {
        var query = _context.OrganizationInvites
            .Include(i => i.CreatedByUser)
            .Include(i => i.UsedBy)
            .Where(i => i.OrganizationId == organizationId && !i.IsDeleted);

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = statusFilter.ToLowerInvariant() switch
            {
                "pending" => query.Where(i => i.UsedAt == null && i.ExpiresAt > DateTime.UtcNow),
                "expired" => query.Where(i => i.UsedAt == null && i.ExpiresAt <= DateTime.UtcNow),
                "accepted" => query.Where(i => i.UsedAt != null),
                _ => query
            };
        }

        var invites = await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invites.Select(MapToDtoSync);
    }

    public async Task<InvitationDto?> GetInvitationAsync(Guid invitationId)
    {
        var invite = await _context.OrganizationInvites
            .Include(i => i.CreatedByUser)
            .Include(i => i.UsedBy)
            .FirstOrDefaultAsync(i => i.Id == invitationId && !i.IsDeleted);

        return invite == null ? null : MapToDtoSync(invite);
    }

    public async Task<InvitationDto?> ValidateCodeAsync(string code)
    {
        // IgnoreQueryFilters: the user accepting the invite is not yet a member
        // of the organization, so the org-scoped query filter would hide the invite.
        var invite = await _context.OrganizationInvites
            .IgnoreQueryFilters()
            .Include(i => i.Organization)
            .Include(i => i.CreatedByUser)
            .FirstOrDefaultAsync(i =>
                i.Code == code &&
                !i.IsDeleted &&
                i.UsedAt == null &&
                i.UseCount < i.MaxUses &&
                i.ExpiresAt > DateTime.UtcNow);

        if (invite == null) return null;

        // Check if the invited email already has an account so the frontend
        // can show "Sign In" vs "Create Account" as the primary action.
        var accountExists = await _context.ApplicationUsers
            .AnyAsync(u => u.Email == invite.Email);

        // Load pending exercise assignments so the invite page can show
        // which exercises the user will be assigned to upon acceptance.
        var pendingExercises = await _context.PendingExerciseAssignments
            .Include(pa => pa.Exercise)
            .Where(pa => pa.OrganizationInviteId == invite.Id
                      && pa.Status == PendingAssignmentStatus.Pending
                      && !pa.Exercise.IsDeleted)
            .Select(pa => new PendingExerciseInfoDto(
                pa.Exercise.Name,
                pa.ExerciseRole.ToString(),
                pa.Exercise.ExerciseType.ToString(),
                pa.Exercise.ScheduledDate))
            .ToListAsync();

        return MapToDtoSync(invite) with
        {
            AccountExists = accountExists,
            PendingExercises = pendingExercises.Count > 0 ? pendingExercises : null
        };
    }

    public async Task AcceptInvitationAsync(string code, string userId)
    {
        // IgnoreQueryFilters: the accepting user is not yet a member of the org.
        var invite = await _context.OrganizationInvites
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i =>
                i.Code == code &&
                !i.IsDeleted &&
                i.UsedAt == null &&
                i.UseCount < i.MaxUses &&
                i.ExpiresAt > DateTime.UtcNow)
            ?? throw new NotFoundException("Invitation not found or has expired");

        // Check if user is already a member
        var existingMembership = await _context.OrganizationMemberships
            .FirstOrDefaultAsync(m =>
                m.UserId == userId &&
                m.OrganizationId == invite.OrganizationId &&
                m.Status == MembershipStatus.Active);

        if (existingMembership != null)
        {
            throw new ConflictException("You are already a member of this organization");
        }

        // Create membership
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = invite.OrganizationId,
            Role = invite.Role,
            Status = MembershipStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrganizationMemberships.Add(membership);

        // Mark invite as used
        invite.UsedAt = DateTime.UtcNow;
        invite.UsedById = userId;
        invite.UseCount++;
        invite.UpdatedAt = DateTime.UtcNow;

        // Switch the user's current organization to the one they just joined
        // so their next JWT will have the correct org context.
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.CurrentOrganizationId = invite.OrganizationId;

            // Set primary org if not yet assigned (new user accepting first invite)
            if (user.OrganizationId == null)
            {
                user.OrganizationId = invite.OrganizationId;
            }

            // Activate the account if it was created in Pending status
            // (e.g. pre-created by a system admin before the user registered).
            if (user.Status == UserStatus.Pending)
            {
                user.Status = UserStatus.Active;
                _logger.LogInformation(
                    "Activated pending user {UserId} upon accepting invitation {InviteId}",
                    userId, invite.Id);
            }
        }

        // Bypass org validation: the user's JWT org context differs from the invited org,
        // but this cross-org write is legitimate (accepting an invitation).
        _context.BypassOrgValidation = true;
        try
        {
            await _context.SaveChangesAsync();
        }
        finally
        {
            _context.BypassOrgValidation = false;
        }

        _logger.LogInformation(
            "Invitation {InviteId} accepted by user {UserId} for organization {OrgId}",
            invite.Id, userId, invite.OrganizationId);

        // Activate any pending exercise assignments linked to this invitation
        await ActivatePendingExerciseAssignmentsAsync(invite.Id, userId, user);
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    /// <summary>
    /// Activates pending exercise assignments when an org invitation is accepted.
    /// Creates ExerciseParticipant records for each pending assignment.
    /// </summary>
    private async Task ActivatePendingExerciseAssignmentsAsync(Guid inviteId, string userId, ApplicationUser? user)
    {
        var pendingAssignments = await _context.PendingExerciseAssignments
            .Include(pa => pa.Exercise)
            .Where(pa => pa.OrganizationInviteId == inviteId && pa.Status == PendingAssignmentStatus.Pending)
            .ToListAsync();

        if (pendingAssignments.Count == 0) return;

        foreach (var assignment in pendingAssignments)
        {
            try
            {
                // Validate exercise is still in Draft or Active status
                if (assignment.Exercise.Status != ExerciseStatus.Draft &&
                    assignment.Exercise.Status != ExerciseStatus.Active)
                {
                    assignment.Status = PendingAssignmentStatus.Cancelled;
                    _logger.LogWarning(
                        "Pending assignment {AssignmentId} cancelled: exercise {ExerciseId} is in {Status} status",
                        assignment.Id, assignment.ExerciseId, assignment.Exercise.Status);
                    continue;
                }

                // Validate Exercise Director role requirement
                var effectiveRole = assignment.ExerciseRole;
                if (effectiveRole == ExerciseRole.ExerciseDirector &&
                    user?.SystemRole == SystemRole.User)
                {
                    // Downgrade to Observer and log warning
                    effectiveRole = ExerciseRole.Observer;
                    _logger.LogWarning(
                        "Pending assignment {AssignmentId}: Exercise Director role downgraded to Observer " +
                        "because user {UserId} has User system role (requires Admin or Manager)",
                        assignment.Id, userId);
                }

                // Check if participant already exists (e.g., added manually between import and acceptance)
                var existingParticipant = await _context.ExerciseParticipants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(ep =>
                        ep.ExerciseId == assignment.ExerciseId &&
                        ep.UserId == userId);

                if (existingParticipant != null)
                {
                    if (existingParticipant.IsDeleted)
                    {
                        // Reactivate soft-deleted participant
                        existingParticipant.IsDeleted = false;
                        existingParticipant.DeletedAt = null;
                        existingParticipant.DeletedBy = null;
                        existingParticipant.Role = effectiveRole;
                        existingParticipant.AssignedAt = DateTime.UtcNow;
                    }
                    // else: already assigned, skip
                }
                else
                {
                    // Create new exercise participant
                    var participant = new ExerciseParticipant
                    {
                        Id = Guid.NewGuid(),
                        ExerciseId = assignment.ExerciseId,
                        UserId = userId,
                        Role = effectiveRole,
                        AssignedAt = DateTime.UtcNow,
                        AssignedById = assignment.CreatedBy
                    };
                    _context.ExerciseParticipants.Add(participant);
                }

                assignment.Status = PendingAssignmentStatus.Activated;

                _logger.LogInformation(
                    "Pending assignment {AssignmentId} activated: user {UserId} assigned as {Role} in exercise {ExerciseId}",
                    assignment.Id, userId, effectiveRole, assignment.ExerciseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to activate pending assignment {AssignmentId} for user {UserId}",
                    assignment.Id, userId);
                // Don't fail the entire invitation acceptance for a failed assignment
            }
        }

        // Save all assignment activations
        // This is within the BypassOrgValidation context from the caller
        _context.BypassOrgValidation = true;
        try
        {
            await _context.SaveChangesAsync();
        }
        finally
        {
            _context.BypassOrgValidation = false;
        }
    }

    /// <summary>
    /// Send invitation email. Logs warnings on failure but doesn't throw.
    /// Returns (sent, errorMessage) so callers can surface the result.
    /// Automatically includes pending exercise assignments if they exist.
    /// </summary>
    private async Task<(bool Sent, string? Error)> SendInvitationEmailAsync(OrganizationInvite invite, string organizationName, string inviterName)
    {
        try
        {
            var inviteUrl = $"{_authOptions.FrontendBaseUrl}/invite/{invite.Code}";

            // Check for pending exercise assignments linked to this invitation
            var pendingAssignments = await _context.PendingExerciseAssignments
                .Include(pa => pa.Exercise)
                .Where(pa => pa.OrganizationInviteId == invite.Id
                          && pa.Status == PendingAssignmentStatus.Pending
                          && !pa.Exercise.IsDeleted)
                .ToListAsync();

            var pendingExercises = pendingAssignments.Select(pa => new PendingExerciseInfo
            {
                ExerciseName = pa.Exercise.Name,
                ExerciseRole = pa.ExerciseRole.ToString(),
                ExerciseType = pa.Exercise.ExerciseType.ToString(),
                ScheduledDate = pa.Exercise.ScheduledDate
            }).ToList();

            // Pre-render exercise lists for email template
            var exercisesHtml = string.Join("", pendingExercises.Select(ex =>
                $@"<div style=""margin: 12px 0; padding: 12px; background-color: #ffffff; border-radius: 4px;"">
    <strong>{ex.ExerciseName}</strong> ({ex.ExerciseType})<br/>
    <span style=""color: #666666;"">Your Role:</span> <strong>{ex.ExerciseRole}</strong><br/>
    <span style=""color: #666666;"">Date:</span> {ex.ScheduledDate:MMMM d, yyyy}
  </div>"));

            var exercisesText = string.Join("\n", pendingExercises.Select(ex =>
                $"- {ex.ExerciseName} ({ex.ExerciseType})\n  Your Role: {ex.ExerciseRole}\n  Date: {ex.ScheduledDate:MMMM d, yyyy}\n"));

            var model = new OrganizationInviteEmailModel
            {
                OrganizationName = organizationName,
                InviterName = inviterName,
                InviteUrl = inviteUrl,
                ExpiresAt = invite.ExpiresAt,
                Role = invite.Role.ToString(),
                PendingExercises = pendingExercises,
                PendingExercisesHtml = exercisesHtml,
                PendingExercisesText = exercisesText
            };

            var recipient = new EmailRecipient(invite.Email ?? string.Empty);

            // Use exercise-aware template if there are pending assignments
            var templateName = pendingAssignments.Count > 0
                ? "OrganizationInviteWithExercises"
                : "OrganizationInvite";

            var result = await _emailService.SendTemplatedAsync(templateName, model, recipient);

            if (result.Status == EmailSendStatus.Failed)
            {
                _logger.LogWarning(
                    "Failed to send invitation email to {Email} for organization {OrgName}. " +
                    "Invitation created but email not delivered. Error: {Error}",
                    invite.Email, organizationName, result.ErrorMessage);
                return (false, result.ErrorMessage);
            }

            _logger.LogInformation(
                "Invitation email sent to {Email} for organization {OrgName} with {ExerciseCount} pending exercise(s). MessageId: {MessageId}",
                invite.Email, organizationName, pendingAssignments.Count, result.MessageId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Exception sending invitation email to {Email} for organization {OrgName}. " +
                "Invitation created but email not delivered.",
                invite.Email, organizationName);
            return (false, ex.Message);
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        var code = new char[InviteCodeLength];
        var bytes = RandomNumberGenerator.GetBytes(InviteCodeLength);

        for (int i = 0; i < InviteCodeLength; i++)
        {
            code[i] = chars[bytes[i] % chars.Length];
        }

        return new string(code);
    }

    private async Task<InvitationDto> MapToDto(OrganizationInvite invite)
    {
        // Load navigation properties if not already loaded
        if (invite.CreatedByUser == null)
        {
            await _context.Entry(invite).Reference(i => i.CreatedByUser).LoadAsync();
        }
        return MapToDtoSync(invite);
    }

    private static InvitationDto MapToDtoSync(OrganizationInvite invite)
    {
        var status = invite.UsedAt != null
            ? "Accepted"
            : invite.IsDeleted
                ? "Cancelled"
                : invite.ExpiresAt <= DateTime.UtcNow
                    ? "Expired"
                    : "Pending";

        return new InvitationDto(
            Id: invite.Id,
            Email: invite.Email ?? string.Empty,
            Code: invite.Code,
            Role: invite.Role.ToString(),
            Status: status,
            CreatedAt: invite.CreatedAt,
            ExpiresAt: invite.ExpiresAt,
            InvitedByName: invite.CreatedByUser?.DisplayName ?? "Unknown",
            InvitedByEmail: invite.CreatedByUser?.Email ?? string.Empty,
            AcceptedAt: invite.UsedAt,
            CancelledAt: invite.IsDeleted ? invite.DeletedAt : null,
            AcceptedByName: invite.UsedBy?.DisplayName,
            OrganizationName: invite.Organization?.Name
        );
    }
}
