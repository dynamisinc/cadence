using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.BulkParticipantImport.Models.Entities;

/// <summary>
/// Represents a deferred exercise role assignment that activates when a participant
/// accepts their organization invitation. Created during bulk import when a participant
/// is not yet an organization member.
/// </summary>
public class PendingExerciseAssignment : BaseEntity
{
    /// <summary>
    /// The organization invite that must be accepted before this assignment activates.
    /// </summary>
    public Guid OrganizationInviteId { get; set; }

    /// <summary>
    /// The exercise to assign the participant to upon invitation acceptance.
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// The HSEEP role to assign in the exercise.
    /// </summary>
    public ExerciseRole ExerciseRole { get; set; }

    /// <summary>
    /// Current status of this pending assignment.
    /// </summary>
    public PendingAssignmentStatus Status { get; set; } = PendingAssignmentStatus.Pending;

    /// <summary>
    /// The bulk import record that created this assignment, if applicable.
    /// </summary>
    public Guid? BulkImportRecordId { get; set; }

    // Navigation properties
    public OrganizationInvite OrganizationInvite { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
    public BulkImportRecord? BulkImportRecord { get; set; }
}
