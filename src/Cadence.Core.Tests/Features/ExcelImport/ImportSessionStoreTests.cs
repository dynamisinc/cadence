using Cadence.Core.Features.ExcelImport.Models;
using Cadence.Core.Features.ExcelImport.Models.DTOs;
using Cadence.Core.Features.ExcelImport.Services;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.ExcelImport;

public class ImportSessionStoreTests
{
    private static ImportSessionStore CreateStore() => new();

    private static ImportSession CreateSession(Guid? id = null, DateTime? expiresAt = null) => new()
    {
        SessionId = id ?? Guid.NewGuid(),
        FileName = "test.xlsx",
        FileFormat = "xlsx",
        TempFilePath = "/tmp/test.xlsx",
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1),
        CurrentStep = "Upload",
        Worksheets = new List<WorksheetInfoDto>()
    };

    // =========================================================================
    // GetSession Tests
    // =========================================================================

    [Fact]
    public void GetSession_ExistingSession_ReturnsSession()
    {
        var store = CreateStore();
        var session = CreateSession();
        store.CreateSession(session);

        var result = store.GetSession(session.SessionId);

        result.Should().NotBeNull();
        result.Should().BeSameAs(session);
    }

    [Fact]
    public void GetSession_NonExistentSession_ReturnsNull()
    {
        var store = CreateStore();

        var result = store.GetSession(Guid.NewGuid());

        result.Should().BeNull();
    }

    // =========================================================================
    // CreateSession Tests
    // =========================================================================

    [Fact]
    public void CreateSession_StoresSession()
    {
        var store = CreateStore();
        var session = CreateSession();

        store.CreateSession(session);

        store.GetSession(session.SessionId).Should().NotBeNull();
    }

    [Fact]
    public void CreateSession_SameId_OverwritesPrevious()
    {
        var store = CreateStore();
        var id = Guid.NewGuid();
        var session1 = CreateSession(id: id);
        session1.CurrentStep = "Upload";
        var session2 = CreateSession(id: id);
        session2.CurrentStep = "Mapping";

        store.CreateSession(session1);
        store.CreateSession(session2);

        var result = store.GetSession(id);
        result.Should().BeSameAs(session2);
        result!.CurrentStep.Should().Be("Mapping");
    }

    [Fact]
    public void CreateSession_MultipleSessions_AllRetrievable()
    {
        var store = CreateStore();
        var session1 = CreateSession();
        var session2 = CreateSession();
        var session3 = CreateSession();

        store.CreateSession(session1);
        store.CreateSession(session2);
        store.CreateSession(session3);

        store.GetSession(session1.SessionId).Should().NotBeNull();
        store.GetSession(session2.SessionId).Should().NotBeNull();
        store.GetSession(session3.SessionId).Should().NotBeNull();
    }

    // =========================================================================
    // RemoveSession Tests
    // =========================================================================

    [Fact]
    public void RemoveSession_ExistingSession_RemovesAndReturnsSession()
    {
        var store = CreateStore();
        var session = CreateSession();
        store.CreateSession(session);

        var removed = store.RemoveSession(session.SessionId);

        removed.Should().BeSameAs(session);
        store.GetSession(session.SessionId).Should().BeNull();
    }

    [Fact]
    public void RemoveSession_NonExistentSession_ReturnsNull()
    {
        var store = CreateStore();

        var removed = store.RemoveSession(Guid.NewGuid());

        removed.Should().BeNull();
    }

    [Fact]
    public void RemoveSession_DoesNotAffectOtherSessions()
    {
        var store = CreateStore();
        var session1 = CreateSession();
        var session2 = CreateSession();
        store.CreateSession(session1);
        store.CreateSession(session2);

        store.RemoveSession(session1.SessionId);

        store.GetSession(session2.SessionId).Should().NotBeNull();
    }

    // =========================================================================
    // CleanupExpiredSessions Tests
    // =========================================================================

    [Fact]
    public void CleanupExpiredSessions_NoExpired_ReturnsEmpty()
    {
        var store = CreateStore();
        var session = CreateSession(expiresAt: DateTime.UtcNow.AddHours(1));
        store.CreateSession(session);

        var removed = store.CleanupExpiredSessions();

        removed.Should().BeEmpty();
        store.GetSession(session.SessionId).Should().NotBeNull();
    }

    [Fact]
    public void CleanupExpiredSessions_ExpiredSession_RemovesAndReturns()
    {
        var store = CreateStore();
        var expired = CreateSession(expiresAt: DateTime.UtcNow.AddHours(-1));
        store.CreateSession(expired);

        var removed = store.CleanupExpiredSessions();

        removed.Should().HaveCount(1);
        removed[0].SessionId.Should().Be(expired.SessionId);
        store.GetSession(expired.SessionId).Should().BeNull();
    }

    [Fact]
    public void CleanupExpiredSessions_MixedSessions_OnlyRemovesExpired()
    {
        var store = CreateStore();
        var expired1 = CreateSession(expiresAt: DateTime.UtcNow.AddHours(-2));
        var expired2 = CreateSession(expiresAt: DateTime.UtcNow.AddMinutes(-1));
        var active = CreateSession(expiresAt: DateTime.UtcNow.AddHours(1));

        store.CreateSession(expired1);
        store.CreateSession(expired2);
        store.CreateSession(active);

        var removed = store.CleanupExpiredSessions();

        removed.Should().HaveCount(2);
        store.GetSession(expired1.SessionId).Should().BeNull();
        store.GetSession(expired2.SessionId).Should().BeNull();
        store.GetSession(active.SessionId).Should().NotBeNull();
    }

    [Fact]
    public void CleanupExpiredSessions_EmptyStore_ReturnsEmpty()
    {
        var store = CreateStore();

        var removed = store.CleanupExpiredSessions();

        removed.Should().BeEmpty();
    }

    // =========================================================================
    // Default Singleton Tests
    // =========================================================================

    [Fact]
    public void Default_IsNotNull()
    {
        ImportSessionStore.Default.Should().NotBeNull();
    }

    [Fact]
    public void Default_IsSameInstance()
    {
        var instance1 = ImportSessionStore.Default;
        var instance2 = ImportSessionStore.Default;

        instance1.Should().BeSameAs(instance2);
    }
}
