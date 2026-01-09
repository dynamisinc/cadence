using FluentAssertions;
using Cadence.Api.Tools.Notes.Models.DTOs;

namespace Cadence.Api.Tests.Models;

public class CreateNoteRequestTests
{
    [Fact]
    public void Validate_WithValidData_DoesNotThrow()
    {
        // Arrange
        var request = new CreateNoteRequest("Valid Title", "Valid Content");

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullContent_DoesNotThrow()
    {
        // Arrange
        var request = new CreateNoteRequest("Valid Title", null);

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyTitle_ThrowsArgumentException(string? title)
    {
        // Arrange
        var request = new CreateNoteRequest(title!, "Content");

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Title is required.*")
            .And.ParamName.Should().Be("Title");
    }

    [Fact]
    public void Validate_WithTitleOver100Characters_ThrowsArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 101);
        var request = new CreateNoteRequest(longTitle, "Content");

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Title must be 100 characters or less.*")
            .And.ParamName.Should().Be("Title");
    }

    [Fact]
    public void Validate_WithTitleExactly100Characters_DoesNotThrow()
    {
        // Arrange
        var exactTitle = new string('a', 100);
        var request = new CreateNoteRequest(exactTitle, "Content");

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithContentOver10000Characters_ThrowsArgumentException()
    {
        // Arrange
        var longContent = new string('a', 10001);
        var request = new CreateNoteRequest("Title", longContent);

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Content must be 10,000 characters or less.*")
            .And.ParamName.Should().Be("Content");
    }

    [Fact]
    public void Validate_WithContentExactly10000Characters_DoesNotThrow()
    {
        // Arrange
        var exactContent = new string('a', 10000);
        var request = new CreateNoteRequest("Title", exactContent);

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }
}

public class UpdateNoteRequestTests
{
    [Fact]
    public void Validate_WithValidData_DoesNotThrow()
    {
        // Arrange
        var request = new UpdateNoteRequest("Valid Title", "Valid Content");

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullContent_DoesNotThrow()
    {
        // Arrange
        var request = new UpdateNoteRequest("Valid Title", null);

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyTitle_ThrowsArgumentException(string? title)
    {
        // Arrange
        var request = new UpdateNoteRequest(title!, "Content");

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Title is required.*")
            .And.ParamName.Should().Be("Title");
    }

    [Fact]
    public void Validate_WithTitleOver100Characters_ThrowsArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 101);
        var request = new UpdateNoteRequest(longTitle, "Content");

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Title must be 100 characters or less.*")
            .And.ParamName.Should().Be("Title");
    }

    [Fact]
    public void Validate_WithContentOver10000Characters_ThrowsArgumentException()
    {
        // Arrange
        var longContent = new string('a', 10001);
        var request = new UpdateNoteRequest("Title", longContent);

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Content must be 10,000 characters or less.*")
            .And.ParamName.Should().Be("Content");
    }
}
