using Cadence.Core.Features.Autocomplete.Mappers;
using Cadence.Core.Features.Autocomplete.Models.DTOs;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Autocomplete.Mappers;

/// <summary>
/// Unit tests for OrganizationSuggestionMapper extension methods.
/// Verifies correct projection of OrganizationSuggestion entities to OrganizationSuggestionDto.
/// </summary>
public class OrganizationSuggestionMapperTests
{
    // =========================================================================
    // ToDto
    // =========================================================================

    [Fact]
    public void ToDto_AllPropertiesPopulated_MapsEveryField()
    {
        // Arrange
        var entity = new OrganizationSuggestion
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            FieldName = SuggestionFieldNames.Source,
            Value = "Fire Department",
            SortOrder = 2,
            IsActive = true,
            IsBlocked = false,
            CreatedAt = new DateTime(2025, 2, 10, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 8, 15, 9, 30, 0, DateTimeKind.Utc)
        };

        // Act
        OrganizationSuggestionDto dto = entity.ToDto();

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.FieldName.Should().Be(entity.FieldName);
        dto.Value.Should().Be(entity.Value);
        dto.SortOrder.Should().Be(entity.SortOrder);
        dto.IsActive.Should().Be(entity.IsActive);
        dto.IsBlocked.Should().Be(entity.IsBlocked);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    [Fact]
    public void ToDto_IsActiveFalse_MapsIsActiveFalse()
    {
        // Arrange
        var entity = new OrganizationSuggestion
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            FieldName = SuggestionFieldNames.Target,
            Value = "EOC",
            SortOrder = 0,
            IsActive = false,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationSuggestionDto dto = entity.ToDto();

        // Assert
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ToDto_IsBlockedTrue_MapsIsBlockedTrue()
    {
        // Arrange
        var entity = new OrganizationSuggestion
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            FieldName = SuggestionFieldNames.Track,
            Value = "Blocked Value",
            SortOrder = 0,
            IsActive = false,
            IsBlocked = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationSuggestionDto dto = entity.ToDto();

        // Assert
        dto.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public void ToDto_SortOrderZero_MapsSortOrderCorrectly()
    {
        // Arrange
        var entity = new OrganizationSuggestion
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            FieldName = SuggestionFieldNames.LocationName,
            Value = "Main EOC",
            SortOrder = 0,
            IsActive = true,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationSuggestionDto dto = entity.ToDto();

        // Assert
        dto.SortOrder.Should().Be(0);
    }

    [Fact]
    public void ToDto_AllValidFieldNames_MapsFieldNameAsIs()
    {
        // Arrange - exercise each known field name constant
        foreach (var fieldName in SuggestionFieldNames.All)
        {
            var entity = new OrganizationSuggestion
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                FieldName = fieldName,
                Value = "Test Value",
                SortOrder = 1,
                IsActive = true,
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            OrganizationSuggestionDto dto = entity.ToDto();

            // Assert
            dto.FieldName.Should().Be(fieldName,
                "field name '{0}' must be preserved exactly", fieldName);
        }
    }

    [Fact]
    public void ToDto_DoesNotExposeOrganizationId()
    {
        // Arrange
        var entity = new OrganizationSuggestion
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            FieldName = SuggestionFieldNames.ResponsibleController,
            Value = "John Smith",
            SortOrder = 0,
            IsActive = true,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationSuggestionDto dto = entity.ToDto();

        // Assert — OrganizationSuggestionDto intentionally omits OrganizationId
        var dtoType = dto.GetType();
        dtoType.GetProperty("OrganizationId").Should().BeNull(
            "OrganizationSuggestionDto must not expose OrganizationId for security");
    }

    [Fact]
    public void ToDto_TimestampsPreserveUtcKind()
    {
        // Arrange
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 30, 23, 59, 59, DateTimeKind.Utc);

        var entity = new OrganizationSuggestion
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            FieldName = SuggestionFieldNames.Source,
            Value = "Test",
            SortOrder = 0,
            IsActive = true,
            IsBlocked = false,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Act
        OrganizationSuggestionDto dto = entity.ToDto();

        // Assert
        dto.CreatedAt.Should().Be(createdAt);
        dto.UpdatedAt.Should().Be(updatedAt);
    }
}
