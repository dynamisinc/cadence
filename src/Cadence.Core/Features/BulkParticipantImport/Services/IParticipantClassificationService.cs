using Cadence.Core.Features.BulkParticipantImport.Models.DTOs;

namespace Cadence.Core.Features.BulkParticipantImport.Services;

/// <summary>
/// Classifies parsed participant rows into Assign, Update, Invite, or Error categories
/// by looking up existing users, org memberships, and exercise assignments in batch.
/// </summary>
public interface IParticipantClassificationService
{
    /// <summary>
    /// Classifies a batch of parsed rows against the current organization and exercise.
    /// Uses batch queries (WHERE email IN) for efficient database lookups.
    /// </summary>
    /// <param name="exerciseId">The target exercise.</param>
    /// <param name="rows">Parsed and validated rows from the file parser.</param>
    /// <returns>Classification results for each row.</returns>
    Task<IReadOnlyList<ClassifiedParticipantRow>> ClassifyAsync(
        Guid exerciseId,
        IReadOnlyList<ParsedParticipantRow> rows);
}
