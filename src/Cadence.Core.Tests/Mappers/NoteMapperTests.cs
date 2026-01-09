using FluentAssertions;
using Cadence.Api.Tools.Notes.Mappers;
using Cadence.Api.Tools.Notes.Models.DTOs;
using Cadence.Api.Tools.Notes.Models.Entities;

namespace Cadence.Api.Tests.Mappers;

public class NoteMapperTests
{
    [Fact]
    public void ToDto_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = "user-123",
            Title = "Test Note",
            Content = "Test Content",
            CreatedAt = now.AddHours(-1),
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        };

        // Act
        var dto = note.ToDto();

        // Assert
        dto.Id.Should().Be(note.Id);
        dto.Title.Should().Be(note.Title);
        dto.Content.Should().Be(note.Content);
        dto.CreatedAt.Should().Be(note.CreatedAt);
        dto.UpdatedAt.Should().Be(note.UpdatedAt);
    }

    [Fact]
    public void ToDto_WithNullContent_MapsCorrectly()
    {
        // Arrange
        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = "user-123",
            Title = "Test Note",
            Content = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var dto = note.ToDto();

        // Assert
        dto.Content.Should().BeNull();
    }

    [Fact]
    public void ToDtos_MapsCollectionCorrectly()
    {
        // Arrange
        var notes = new List<Note>
        {
            new() { Id = Guid.NewGuid(), UserId = "user-1", Title = "Note 1", Content = "Content 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = "user-2", Title = "Note 2", Content = "Content 2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = "user-3", Title = "Note 3", Content = null, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        // Act
        var dtos = notes.ToDtos().ToList();

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].Title.Should().Be("Note 1");
        dtos[1].Title.Should().Be("Note 2");
        dtos[2].Title.Should().Be("Note 3");
        dtos[2].Content.Should().BeNull();
    }

    [Fact]
    public void ToDtos_WithEmptyCollection_ReturnsEmptyCollection()
    {
        // Arrange
        var notes = new List<Note>();

        // Act
        var dtos = notes.ToDtos().ToList();

        // Assert
        dtos.Should().BeEmpty();
    }

    [Fact]
    public void ToEntity_CreatesNoteWithCorrectProperties()
    {
        // Arrange
        var userId = "user-123";
        var request = new CreateNoteRequest("  Test Title  ", "  Test Content  ");

        // Act
        var beforeCreation = DateTime.UtcNow;
        var note = request.ToEntity(userId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        note.Id.Should().NotBeEmpty();
        note.UserId.Should().Be(userId);
        note.Title.Should().Be("Test Title"); // Should be trimmed
        note.Content.Should().Be("Test Content"); // Should be trimmed
        note.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        note.UpdatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        note.IsDeleted.Should().BeFalse();
        note.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void ToEntity_WithNullContent_CreatesNoteWithNullContent()
    {
        // Arrange
        var userId = "user-123";
        var request = new CreateNoteRequest("Title", null);

        // Act
        var note = request.ToEntity(userId);

        // Assert
        note.Content.Should().BeNull();
    }

    [Fact]
    public void ToEntity_GeneratesUniqueIds()
    {
        // Arrange
        var request = new CreateNoteRequest("Title", "Content");

        // Act
        var note1 = request.ToEntity("user-1");
        var note2 = request.ToEntity("user-2");

        // Assert
        note1.Id.Should().NotBe(note2.Id);
    }

    [Fact]
    public void UpdateFrom_UpdatesPropertiesCorrectly()
    {
        // Arrange
        var originalCreatedAt = DateTime.UtcNow.AddDays(-1);
        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = "user-123",
            Title = "Original Title",
            Content = "Original Content",
            CreatedAt = originalCreatedAt,
            UpdatedAt = originalCreatedAt
        };

        var request = new UpdateNoteRequest("  Updated Title  ", "  Updated Content  ");

        // Act
        var beforeUpdate = DateTime.UtcNow;
        note.UpdateFrom(request);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        note.Title.Should().Be("Updated Title"); // Should be trimmed
        note.Content.Should().Be("Updated Content"); // Should be trimmed
        note.UpdatedAt.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
        note.CreatedAt.Should().Be(originalCreatedAt); // Should not change
    }

    [Fact]
    public void UpdateFrom_WithNullContent_SetsContentToNull()
    {
        // Arrange
        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = "user-123",
            Title = "Title",
            Content = "Original Content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateNoteRequest("Updated Title", null);

        // Act
        note.UpdateFrom(request);

        // Assert
        note.Content.Should().BeNull();
    }

    [Fact]
    public void UpdateFrom_PreservesIdAndUserId()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var originalUserId = "user-123";
        var note = new Note
        {
            Id = originalId,
            UserId = originalUserId,
            Title = "Title",
            Content = "Content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateNoteRequest("New Title", "New Content");

        // Act
        note.UpdateFrom(request);

        // Assert
        note.Id.Should().Be(originalId);
        note.UserId.Should().Be(originalUserId);
    }
}
