using Cadence.Core.Features.Autocomplete.Mappers;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Mappers;

public class OrganizationSuggestionMapperTests
{
    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var entity = new OrganizationSuggestion
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            FieldName = "Source",
            Value = "Fire Department",
            SortOrder = 1,
            IsActive = true,
            IsBlocked = false,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var dto = entity.ToDto();

        dto.Id.Should().Be(entity.Id);
        dto.FieldName.Should().Be("Source");
        dto.Value.Should().Be("Fire Department");
        dto.SortOrder.Should().Be(1);
        dto.IsActive.Should().BeTrue();
        dto.IsBlocked.Should().BeFalse();
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    [Fact]
    public void ToDto_BlockedSuggestion_MapsIsBlocked()
    {
        var entity = new OrganizationSuggestion
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            FieldName = "Target",
            Value = "Spam Value",
            SortOrder = 0,
            IsActive = false,
            IsBlocked = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = entity.ToDto();

        dto.IsActive.Should().BeFalse();
        dto.IsBlocked.Should().BeTrue();
    }
}
