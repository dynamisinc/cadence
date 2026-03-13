using System.Collections.Concurrent;
using Cadence.Core.Features.ExcelImport.Models;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// In-memory singleton store for active Excel import sessions.
/// </summary>
/// <remarks>
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread safety. The store
/// must be registered as a singleton in DI so that session state persists across
/// multiple HTTP requests within the same process.
///
/// LIMITATION: This does not work in multi-instance deployments (Azure Scale Out, K8s
/// replicas). For production environments with multiple instances, replace with a
/// Redis- or SQL-backed implementation.
///
/// The static <see cref="Default"/> instance allows <see cref="ExcelImportService"/> to
/// be constructed without DI (e.g., directly in unit tests) while still sharing the
/// same underlying dictionary that DI-resolved instances use.
/// </remarks>
internal sealed class ImportSessionStore : IImportSessionStore
{
    /// <summary>
    /// A process-wide singleton used when no DI-provided instance is available.
    /// This ensures backward compatibility with tests that construct
    /// <see cref="ExcelImportService"/> directly without injecting a session store.
    /// </summary>
    public static readonly ImportSessionStore Default = new();

    private readonly ConcurrentDictionary<Guid, ImportSession> _sessions = new();

    /// <inheritdoc />
    public ImportSession? GetSession(Guid sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <inheritdoc />
    public void CreateSession(ImportSession session)
    {
        _sessions[session.SessionId] = session;
    }

    /// <inheritdoc />
    public ImportSession? RemoveSession(Guid sessionId)
    {
        _sessions.TryRemove(sessionId, out var session);
        return session;
    }

    /// <inheritdoc />
    public IReadOnlyList<ImportSession> CleanupExpiredSessions()
    {
        var now = DateTime.UtcNow;

        var expiredKeys = _sessions
            .Where(kv => kv.Value.ExpiresAt < now)
            .Select(kv => kv.Key)
            .ToList();

        var removed = new List<ImportSession>(expiredKeys.Count);
        foreach (var key in expiredKeys)
        {
            if (_sessions.TryRemove(key, out var session))
            {
                removed.Add(session);
            }
        }

        return removed;
    }
}
