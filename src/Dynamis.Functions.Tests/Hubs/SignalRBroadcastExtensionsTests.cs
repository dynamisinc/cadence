using FluentAssertions;
using DynamisReferenceApp.Api.Hubs;

namespace DynamisReferenceApp.Api.Tests.Hubs;

public class SignalRBroadcastExtensionsTests
{
    [Fact]
    public void NoteCreated_ReturnsCorrectMessageAction()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var userId = "user-123";

        // Act
        var beforeCall = DateTime.UtcNow;
        var result = SignalRBroadcastExtensions.NoteCreated(noteId, userId);
        var afterCall = DateTime.UtcNow;

        // Assert
        result.Should().NotBeNull();
        result.Target.Should().Be("noteCreated");
        result.Arguments.Should().HaveCount(1);

        var arg = result.Arguments![0];
        arg.Should().BeEquivalentTo(new
        {
            noteId,
            userId
        }, options => options.Excluding(x => x.Name == "timestamp"));

        // Verify timestamp is in valid range
        var timestamp = (DateTime)arg.GetType().GetProperty("timestamp")!.GetValue(arg)!;
        timestamp.Should().BeOnOrAfter(beforeCall).And.BeOnOrBefore(afterCall);
    }

    [Fact]
    public void NoteUpdated_ReturnsCorrectMessageAction()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var userId = "user-456";

        // Act
        var beforeCall = DateTime.UtcNow;
        var result = SignalRBroadcastExtensions.NoteUpdated(noteId, userId);
        var afterCall = DateTime.UtcNow;

        // Assert
        result.Should().NotBeNull();
        result.Target.Should().Be("noteUpdated");
        result.Arguments.Should().HaveCount(1);

        var arg = result.Arguments![0];
        arg.Should().BeEquivalentTo(new
        {
            noteId,
            userId
        }, options => options.Excluding(x => x.Name == "timestamp"));

        var timestamp = (DateTime)arg.GetType().GetProperty("timestamp")!.GetValue(arg)!;
        timestamp.Should().BeOnOrAfter(beforeCall).And.BeOnOrBefore(afterCall);
    }

    [Fact]
    public void NoteDeleted_ReturnsCorrectMessageAction()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var userId = "user-789";

        // Act
        var beforeCall = DateTime.UtcNow;
        var result = SignalRBroadcastExtensions.NoteDeleted(noteId, userId);
        var afterCall = DateTime.UtcNow;

        // Assert
        result.Should().NotBeNull();
        result.Target.Should().Be("noteDeleted");
        result.Arguments.Should().HaveCount(1);

        var arg = result.Arguments![0];
        arg.Should().BeEquivalentTo(new
        {
            noteId,
            userId
        }, options => options.Excluding(x => x.Name == "timestamp"));

        var timestamp = (DateTime)arg.GetType().GetProperty("timestamp")!.GetValue(arg)!;
        timestamp.Should().BeOnOrAfter(beforeCall).And.BeOnOrBefore(afterCall);
    }

    [Fact]
    public void NoteCreated_GeneratesNewTimestampOnEachCall()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var userId = "user-123";

        // Act
        var result1 = SignalRBroadcastExtensions.NoteCreated(noteId, userId);
        Thread.Sleep(10); // Small delay to ensure different timestamps
        var result2 = SignalRBroadcastExtensions.NoteCreated(noteId, userId);

        // Assert
        var timestamp1 = (DateTime)result1.Arguments![0].GetType().GetProperty("timestamp")!.GetValue(result1.Arguments[0])!;
        var timestamp2 = (DateTime)result2.Arguments![0].GetType().GetProperty("timestamp")!.GetValue(result2.Arguments[0])!;

        timestamp2.Should().BeOnOrAfter(timestamp1);
    }

    [Fact]
    public void NoteCreated_WithEmptyGuid_StillCreatesValidMessage()
    {
        // Arrange
        var noteId = Guid.Empty;
        var userId = "user-123";

        // Act
        var result = SignalRBroadcastExtensions.NoteCreated(noteId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Target.Should().Be("noteCreated");

        var arg = result.Arguments![0];
        var actualNoteId = (Guid)arg.GetType().GetProperty("noteId")!.GetValue(arg)!;
        actualNoteId.Should().Be(Guid.Empty);
    }
}
