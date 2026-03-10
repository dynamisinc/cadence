using Cadence.Core.Features.ExcelImport.Models;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// Manages in-memory storage for active Excel import sessions.
/// Implementations are expected to be singletons because session state must outlive
/// any individual HTTP request scope.
/// </summary>
/// <remarks>
/// TODO (CD-C01): Replace with an IDistributedCache-backed implementation (Redis or SQL)
/// so sessions survive App Service restarts and multi-instance scale-out. The current
/// in-memory implementation is only safe for single-instance deployments.
/// </remarks>
public interface IImportSessionStore
{
    /// <summary>
    /// Retrieves an active session by ID.
    /// Returns <c>null</c> if the session does not exist or has expired.
    /// </summary>
    ImportSession? GetSession(Guid sessionId);

    /// <summary>
    /// Stores a new session.
    /// </summary>
    void CreateSession(ImportSession session);

    /// <summary>
    /// Removes a session and returns the removed instance, or <c>null</c> if it was not found.
    /// </summary>
    ImportSession? RemoveSession(Guid sessionId);

    /// <summary>
    /// Removes all sessions whose <see cref="ImportSession.ExpiresAt"/> is in the past.
    /// Called in a background fire-and-forget task after each upload.
    /// </summary>
    IReadOnlyList<ImportSession> CleanupExpiredSessions();
}
