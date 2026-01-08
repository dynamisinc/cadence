using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using DynamisReferenceApp.Api.Tests.Helpers;
using DynamisReferenceApp.Api.Tools.Notes.Models.DTOs;
using DynamisReferenceApp.Api.Tools.Notes.Models.Entities;
using DynamisReferenceApp.Api.Tools.Notes.Services;

namespace DynamisReferenceApp.Api.Tests.Services;

public class NotesServiceTests
{
    private readonly Mock<ILogger<NotesService>> _loggerMock;
    private readonly Mock<IValidator<CreateNoteRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateNoteRequest>> _updateValidatorMock;
    private const string TestUserId = "test-user-123";

    public NotesServiceTests()
    {
        _loggerMock = new Mock<ILogger<NotesService>>();
        _createValidatorMock = new Mock<IValidator<CreateNoteRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateNoteRequest>>();
    }

    [Fact]
    public async Task GetNotesAsync_ReturnsUserNotes()
    {
        // Arrange
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.AddRange(
                new Note { Id = Guid.NewGuid(), Title = "Note 1", Content = "Content 1", UserId = TestUserId },
                new Note { Id = Guid.NewGuid(), Title = "Note 2", Content = "Content 2", UserId = TestUserId },
                new Note { Id = Guid.NewGuid(), Title = "Other User Note", Content = "Content", UserId = "other-user" }
            );
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.GetNotesAsync(TestUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(n => n.Title.Contains("Note"));
    }

    [Fact]
    public async Task GetNoteAsync_ReturnsNote_WhenExistsAndOwned()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note { Id = noteId, Title = "Test Note", Content = "Test Content", UserId = TestUserId });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.GetNoteAsync(noteId, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Note");
        result.Content.Should().Be("Test Content");
    }

    [Fact]
    public async Task GetNoteAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.GetNoteAsync(Guid.NewGuid(), TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNoteAsync_ReturnsNull_WhenOwnedByOtherUser()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note { Id = noteId, Title = "Test Note", Content = "Content", UserId = "other-user" });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.GetNoteAsync(noteId, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateNoteAsync_CreatesNote_AndReturnsDto()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        var request = new CreateNoteRequest("New Note", "New Content");

        // Act
        var result = await service.CreateNoteAsync(request, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Note");
        result.Content.Should().Be("New Content");
        result.Id.Should().NotBeEmpty();

        // Verify persisted
        var persisted = await context.Notes.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task UpdateNoteAsync_UpdatesNote_WhenExistsAndOwned()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note
            {
                Id = noteId,
                Title = "Original Title",
                Content = "Original Content",
                UserId = TestUserId
            });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);
        var request = new UpdateNoteRequest("Updated Title", "Updated Content");

        // Act
        var result = await service.UpdateNoteAsync(noteId, request, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Content.Should().Be("Updated Content");
    }

    [Fact]
    public async Task UpdateNoteAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);
        var request = new UpdateNoteRequest("Title", "Content");

        // Act
        var result = await service.UpdateNoteAsync(Guid.NewGuid(), request, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateNoteAsync_ReturnsNull_WhenOwnedByOtherUser()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note { Id = noteId, Title = "Note", Content = "Content", UserId = "other-user" });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);
        var request = new UpdateNoteRequest("Title", "Content");

        // Act
        var result = await service.UpdateNoteAsync(noteId, request, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteNoteAsync_SoftDeletesNote_WhenExistsAndOwned()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note { Id = noteId, Title = "To Delete", Content = "Content", UserId = TestUserId });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.DeleteNoteAsync(noteId, TestUserId);

        // Assert
        result.Should().BeTrue();

        // Verify soft deleted
        var deleted = await context.Notes.FindAsync(noteId);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteNoteAsync_ReturnsFalse_WhenNotExists()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.DeleteNoteAsync(Guid.NewGuid(), TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreNoteAsync_RestoresDeletedNote()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note
            {
                Id = noteId,
                Title = "Deleted Note",
                Content = "Content",
                UserId = TestUserId,
                IsDeleted = true
            });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.RestoreNoteAsync(noteId, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Deleted Note");

        // Verify restored
        var restored = await context.Notes.FindAsync(noteId);
        restored!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreNoteAsync_ReturnsNull_WhenNoteIsNotDeleted()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note
            {
                Id = noteId,
                Title = "Active Note",
                Content = "Content",
                UserId = TestUserId,
                IsDeleted = false
            });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.RestoreNoteAsync(noteId, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreNoteAsync_ReturnsNull_WhenNoteNotExists()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.RestoreNoteAsync(Guid.NewGuid(), TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreNoteAsync_ReturnsNull_WhenOwnedByOtherUser()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note
            {
                Id = noteId,
                Title = "Other User Note",
                Content = "Content",
                UserId = "other-user",
                IsDeleted = true
            });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.RestoreNoteAsync(noteId, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNotesAsync_ExcludesDeletedNotes()
    {
        // Arrange
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.AddRange(
                new Note { Id = Guid.NewGuid(), Title = "Active Note", Content = "Content", UserId = TestUserId, IsDeleted = false },
                new Note { Id = Guid.NewGuid(), Title = "Deleted Note", Content = "Content", UserId = TestUserId, IsDeleted = true }
            );
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.GetNotesAsync(TestUserId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(n => n.Title == "Active Note");
    }

    [Fact]
    public async Task GetNotesAsync_ReturnsNotesOrderedByUpdatedAtDescending()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var oldNoteTime = now.AddDays(-2);
        var middleNoteTime = now.AddDays(-1);
        var newestNoteTime = now;

        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.AddRange(
                new Note { Id = Guid.NewGuid(), Title = "Old Note", Content = "Content", UserId = TestUserId, CreatedAt = oldNoteTime, UpdatedAt = oldNoteTime },
                new Note { Id = Guid.NewGuid(), Title = "Newest Note", Content = "Content", UserId = TestUserId, CreatedAt = newestNoteTime, UpdatedAt = newestNoteTime },
                new Note { Id = Guid.NewGuid(), Title = "Middle Note", Content = "Content", UserId = TestUserId, CreatedAt = middleNoteTime, UpdatedAt = middleNoteTime }
            );
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = (await service.GetNotesAsync(TestUserId)).ToList();

        // Assert
        result.Should().HaveCount(3);
        // Verify ordering by checking that UpdatedAt values are in descending order
        result.Should().BeInDescendingOrder(n => n.UpdatedAt);
    }

    [Fact]
    public async Task GetNotesAsync_ReturnsEmptyWhenNoNotes()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.GetNotesAsync(TestUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNoteAsync_ReturnsNull_WhenNoteIsDeleted()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note
            {
                Id = noteId,
                Title = "Deleted Note",
                Content = "Content",
                UserId = TestUserId,
                IsDeleted = true
            });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.GetNoteAsync(noteId, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateNoteAsync_ThrowsOnInvalidRequest()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);
        var request = new CreateNoteRequest("", "Content"); // Empty title
        var failures = new[] { new FluentValidation.Results.ValidationFailure("Title", "Error") };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateNoteRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(failures));

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(failures));

        // Act
        var act = () => service.CreateNoteAsync(request, TestUserId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateNoteAsync_ThrowsOnInvalidRequest()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note { Id = noteId, Title = "Note", Content = "Content", UserId = TestUserId });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);
        var request = new UpdateNoteRequest("", "Content"); // Empty title
        var failures = new[] { new FluentValidation.Results.ValidationFailure("Title", "Error") };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateNoteRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(failures));

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(failures));

        // Act
        var act = () => service.UpdateNoteAsync(noteId, request, TestUserId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateNoteAsync_ReturnsNull_WhenNoteIsDeleted()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note
            {
                Id = noteId,
                Title = "Deleted Note",
                Content = "Content",
                UserId = TestUserId,
                IsDeleted = true
            });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);
        var request = new UpdateNoteRequest("Updated Title", "Updated Content");

        // Act
        var result = await service.UpdateNoteAsync(noteId, request, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteNoteAsync_ReturnsFalse_WhenNoteIsAlreadyDeleted()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note
            {
                Id = noteId,
                Title = "Already Deleted",
                Content = "Content",
                UserId = TestUserId,
                IsDeleted = true
            });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.DeleteNoteAsync(noteId, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteNoteAsync_ReturnsFalse_WhenOwnedByOtherUser()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note { Id = noteId, Title = "Note", Content = "Content", UserId = "other-user" });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var result = await service.DeleteNoteAsync(noteId, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteNoteAsync_SetsDeletedAtTimestamp()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note { Id = noteId, Title = "To Delete", Content = "Content", UserId = TestUserId });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var beforeDelete = DateTime.UtcNow;
        await service.DeleteNoteAsync(noteId, TestUserId);
        var afterDelete = DateTime.UtcNow;

        // Assert
        var deleted = await context.Notes.FindAsync(noteId);
        deleted!.DeletedAt.Should().NotBeNull();
        deleted.DeletedAt.Should().BeOnOrAfter(beforeDelete).And.BeOnOrBefore(afterDelete);
        deleted.UpdatedAt.Should().BeOnOrAfter(beforeDelete).And.BeOnOrBefore(afterDelete);
    }

    [Fact]
    public async Task RestoreNoteAsync_ClearsDeletedAtTimestamp()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var deletedAt = DateTime.UtcNow.AddHours(-1);
        var context = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notes.Add(new Note
            {
                Id = noteId,
                Title = "Deleted Note",
                Content = "Content",
                UserId = TestUserId,
                IsDeleted = true,
                DeletedAt = deletedAt
            });
        });

        var service = new NotesService(context, _loggerMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);

        // Act
        var beforeRestore = DateTime.UtcNow;
        await service.RestoreNoteAsync(noteId, TestUserId);
        var afterRestore = DateTime.UtcNow;

        // Assert
        var restored = await context.Notes.FindAsync(noteId);
        restored!.DeletedAt.Should().BeNull();
        restored.IsDeleted.Should().BeFalse();
        restored.UpdatedAt.Should().BeOnOrAfter(beforeRestore).And.BeOnOrBefore(afterRestore);
    }
}
