using Cadence.Core.Data;
using Cadence.Core.Features.BulkParticipantImport.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.BulkParticipantImport.Services;

/// <summary>
/// Classifies parsed participant rows into Assign, Update, Invite, or Error categories
/// by looking up existing users, org memberships, and exercise assignments in batch.
/// </summary>
public class ParticipantClassificationService : IParticipantClassificationService
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly ILogger<ParticipantClassificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParticipantClassificationService"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="orgContext">Current organization context.</param>
    /// <param name="logger">Logger instance.</param>
    public ParticipantClassificationService(
        AppDbContext context,
        ICurrentOrganizationContext orgContext,
        ILogger<ParticipantClassificationService> logger)
    {
        _context = context;
        _orgContext = orgContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClassifiedParticipantRow>> ClassifyAsync(
        Guid exerciseId,
        IReadOnlyList<ParsedParticipantRow> rows)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var orgId = _orgContext.CurrentOrganizationId
            ?? throw new InvalidOperationException("Organization context is required");

        _logger.LogInformation(
            "Classifying {RowCount} parsed rows for exercise {ExerciseId} in org {OrganizationId}",
            rows.Count, exerciseId, orgId);

        // Separate valid rows from rows with validation errors
        var validRows = rows.Where(r => r.IsValid).ToList();
        var errorRows = rows.Where(r => !r.IsValid).ToList();

        var classified = new List<ClassifiedParticipantRow>();

        // Process rows with validation errors first
        foreach (var errorRow in errorRows)
        {
            classified.Add(new ClassifiedParticipantRow
            {
                ParsedRow = errorRow,
                Classification = ParticipantClassification.Error,
                ClassificationLabel = "Error",
                ErrorMessage = string.Join("; ", errorRow.ValidationErrors)
            });
        }

        if (validRows.Count == 0)
        {
            return classified;
        }

        // Batch query: Collect all unique emails (normalized to lowercase)
        var emails = validRows
            .Select(r => r.Email.ToLowerInvariant())
            .Distinct()
            .ToList();

        // Batch query: Find all users by normalized email
        var users = await _context.ApplicationUsers
            .Where(u => emails.Contains(u.NormalizedEmail!.ToLowerInvariant()))
            .ToDictionaryAsync(u => u.NormalizedEmail!.ToLowerInvariant(), u => u);

        // Batch query: Find all active org memberships for these users in current org
        var userIds = users.Values.Select(u => u.Id).ToList();
        var memberships = await _context.OrganizationMemberships
            .Where(m => userIds.Contains(m.UserId)
                && m.OrganizationId == orgId
                && m.Status == MembershipStatus.Active)
            .ToDictionaryAsync(m => m.UserId, m => m);

        // Batch query: Find all exercise participants (including soft-deleted)
        var participants = await _context.ExerciseParticipants
            .IgnoreQueryFilters()
            .Where(p => p.ExerciseId == exerciseId && userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p);

        // Batch query: Find all pending org invites for these emails
        var pendingInvites = await _context.OrganizationInvites
            .Where(i => i.OrganizationId == orgId
                && emails.Contains(i.Email!.ToLowerInvariant())
                && i.UsedAt == null
                && i.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var pendingInviteEmails = pendingInvites
            .Select(i => i.Email!.ToLowerInvariant())
            .ToHashSet();

        // Classify each valid row
        foreach (var row in validRows)
        {
            var normalizedEmail = row.Email.ToLowerInvariant();
            var user = users.GetValueOrDefault(normalizedEmail);
            var hasMembership = user != null && memberships.ContainsKey(user.Id);
            var participant = user != null ? participants.GetValueOrDefault(user.Id) : null;
            var hasPendingInvite = pendingInviteEmails.Contains(normalizedEmail);

            classified.Add(ClassifyRow(row, user, hasMembership, participant, hasPendingInvite));
        }

        _logger.LogInformation(
            "Classification complete: {AssignCount} Assign, {UpdateCount} Update, {InviteCount} Invite, {ErrorCount} Error",
            classified.Count(c => c.Classification == ParticipantClassification.Assign),
            classified.Count(c => c.Classification == ParticipantClassification.Update),
            classified.Count(c => c.Classification == ParticipantClassification.Invite),
            classified.Count(c => c.Classification == ParticipantClassification.Error));

        return classified;
    }

    private ClassifiedParticipantRow ClassifyRow(
        ParsedParticipantRow row,
        ApplicationUser? user,
        bool hasMembership,
        ExerciseParticipant? participant,
        bool hasPendingInvite)
    {
        // Error case: row has validation errors (already handled above, but defensive)
        if (!row.IsValid)
        {
            return new ClassifiedParticipantRow
            {
                ParsedRow = row,
                Classification = ParticipantClassification.Error,
                ClassificationLabel = "Error",
                ErrorMessage = string.Join("; ", row.ValidationErrors)
            };
        }

        // Exercise Director role validation for existing users
        if (row.NormalizedExerciseRole == ExerciseRole.ExerciseDirector
            && user != null
            && hasMembership
            && user.SystemRole == SystemRole.User)
        {
            return new ClassifiedParticipantRow
            {
                ParsedRow = row,
                Classification = ParticipantClassification.Error,
                ClassificationLabel = "Error",
                ExistingUserId = user.Id,
                ExistingDisplayName = user.DisplayName,
                ErrorMessage = "Exercise Director role requires Admin or Manager system role"
            };
        }

        // Update case: existing participant in exercise
        if (user != null && hasMembership && participant != null && !participant.IsDeleted)
        {
            var isRoleChange = participant.Role != row.NormalizedExerciseRole;
            return new ClassifiedParticipantRow
            {
                ParsedRow = row,
                Classification = ParticipantClassification.Update,
                ClassificationLabel = isRoleChange ? "Update - Role Change" : "Update - No Change",
                ExistingUserId = user.Id,
                ExistingDisplayName = user.DisplayName,
                CurrentExerciseRole = participant.Role,
                IsRoleChange = isRoleChange
            };
        }

        // Assign case: existing org member not in exercise (or soft-deleted participant)
        if (user != null && hasMembership)
        {
            var assignNotes = new List<string>();
            if (participant?.IsDeleted == true)
            {
                assignNotes.Add("Will reactivate previous participation");
            }

            return new ClassifiedParticipantRow
            {
                ParsedRow = row,
                Classification = ParticipantClassification.Assign,
                ClassificationLabel = "Assign",
                ExistingUserId = user.Id,
                ExistingDisplayName = user.DisplayName,
                Notes = assignNotes
            };
        }

        // Invite case: user exists in Cadence but not in org, OR completely new user
        var isNewAccount = user == null;
        var inviteNotes = new List<string>();

        if (hasPendingInvite)
        {
            inviteNotes.Add("Existing pending invitation will be updated with exercise assignment");
        }

        return new ClassifiedParticipantRow
        {
            ParsedRow = row,
            Classification = ParticipantClassification.Invite,
            ClassificationLabel = isNewAccount ? "Invite (New Account)" : "Invite",
            ExistingUserId = user?.Id,
            ExistingDisplayName = user?.DisplayName,
            IsNewAccount = isNewAccount,
            HasPendingInvitation = hasPendingInvite,
            Notes = inviteNotes
        };
    }
}
