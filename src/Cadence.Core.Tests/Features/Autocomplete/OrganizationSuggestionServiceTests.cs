using Cadence.Core.Data;
using Cadence.Core.Features.Autocomplete.Services;
using Cadence.Core.Features.Autocomplete.Models.DTOs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Autocomplete;

/// <summary>
/// Tests for OrganizationSuggestionService — CRUD, bulk operations, blocking,
/// reordering, and validation for organization-curated autocomplete suggestions.
/// </summary>
public class OrganizationSuggestionServiceTests
{
    private (AppDbContext context, OrganizationSuggestionService service, Guid orgId) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            Slug = "test-org"
        };
        context.Organizations.Add(org);
        context.SaveChanges();

        var service = new OrganizationSuggestionService(context);
        return (context, service, org.Id);
    }

    // =========================================================================
    // GetSuggestionsAsync
    // =========================================================================

    [Fact]
    public async Task GetSuggestionsAsync_InvalidFieldName_ThrowsArgumentException()
    {
        var (_, service, orgId) = CreateTestContext();

        var act = () => service.GetSuggestionsAsync(orgId, "InvalidField");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSuggestionsAsync_NoSuggestions_ReturnsEmpty()
    {
        var (_, service, orgId) = CreateTestContext();

        var result = await service.GetSuggestionsAsync(orgId, SuggestionFieldNames.Track);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ActiveOnly_ExcludesInactive()
    {
        var (_, service, orgId) = CreateTestContext();
        await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Active" });

        // Create then deactivate
        var inactive = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Inactive" });
        await service.UpdateSuggestionAsync(orgId, inactive.Id,
            new UpdateSuggestionRequest { Value = "Inactive", SortOrder = 0, IsActive = false });

        var result = await service.GetSuggestionsAsync(orgId, SuggestionFieldNames.Track);

        result.Should().ContainSingle(s => s.Value == "Active");
    }

    [Fact]
    public async Task GetSuggestionsAsync_IncludeInactive_ReturnsAll()
    {
        var (_, service, orgId) = CreateTestContext();
        await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Active" });

        var inactive = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Inactive" });
        await service.UpdateSuggestionAsync(orgId, inactive.Id,
            new UpdateSuggestionRequest { Value = "Inactive", SortOrder = 0, IsActive = false });

        var result = await service.GetSuggestionsAsync(orgId, SuggestionFieldNames.Track, includeInactive: true);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSuggestionsAsync_OrderedBySortOrderThenValue()
    {
        var (_, service, orgId) = CreateTestContext();
        await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Bravo", SortOrder = 1 });
        await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Alpha", SortOrder = 1 });
        await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Charlie", SortOrder = 0 });

        var result = (await service.GetSuggestionsAsync(orgId, SuggestionFieldNames.Track)).ToList();

        result[0].Value.Should().Be("Charlie");  // SortOrder 0
        result[1].Value.Should().Be("Alpha");     // SortOrder 1, alphabetical
        result[2].Value.Should().Be("Bravo");     // SortOrder 1, alphabetical
    }

    // =========================================================================
    // GetSuggestionAsync
    // =========================================================================

    [Fact]
    public async Task GetSuggestionAsync_NonexistentId_ReturnsNull()
    {
        var (_, service, orgId) = CreateTestContext();

        var result = await service.GetSuggestionAsync(orgId, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSuggestionAsync_ExistingId_ReturnsDto()
    {
        var (_, service, orgId) = CreateTestContext();
        var created = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Test Track" });

        var result = await service.GetSuggestionAsync(orgId, created.Id);

        result.Should().NotBeNull();
        result!.Value.Should().Be("Test Track");
    }

    [Fact]
    public async Task GetSuggestionAsync_WrongOrg_ReturnsNull()
    {
        var (_, service, orgId) = CreateTestContext();
        var created = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Test" });

        var result = await service.GetSuggestionAsync(Guid.NewGuid(), created.Id);

        result.Should().BeNull();
    }

    // =========================================================================
    // CreateSuggestionAsync
    // =========================================================================

    [Fact]
    public async Task CreateSuggestionAsync_ValidRequest_ReturnsDtoWithCorrectValues()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new CreateSuggestionRequest
        {
            FieldName = SuggestionFieldNames.Source,
            Value = "Dispatch Center",
            SortOrder = 5
        };

        var result = await service.CreateSuggestionAsync(orgId, request);

        result.FieldName.Should().Be(SuggestionFieldNames.Source);
        result.Value.Should().Be("Dispatch Center");
        result.SortOrder.Should().Be(5);
        result.IsActive.Should().BeTrue();
        result.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSuggestionAsync_InvalidFieldName_ThrowsArgumentException()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new CreateSuggestionRequest { FieldName = "BadField", Value = "Test" };

        var act = () => service.CreateSuggestionAsync(orgId, request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateSuggestionAsync_EmptyValue_ThrowsArgumentException()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "   " };

        var act = () => service.CreateSuggestionAsync(orgId, request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task CreateSuggestionAsync_DuplicateValue_ThrowsInvalidOperationException()
    {
        var (_, service, orgId) = CreateTestContext();
        await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Alpha" });

        var act = () => service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "alpha" }); // case-insensitive

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateSuggestionAsync_TrimsWhitespace()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new CreateSuggestionRequest
        {
            FieldName = SuggestionFieldNames.Track,
            Value = "  Trimmed Value  "
        };

        var result = await service.CreateSuggestionAsync(orgId, request);

        result.Value.Should().Be("Trimmed Value");
    }

    // =========================================================================
    // UpdateSuggestionAsync
    // =========================================================================

    [Fact]
    public async Task UpdateSuggestionAsync_NonexistentId_ReturnsNull()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new UpdateSuggestionRequest { Value = "Updated", SortOrder = 0, IsActive = true };

        var result = await service.UpdateSuggestionAsync(orgId, Guid.NewGuid(), request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSuggestionAsync_ValidRequest_UpdatesAllFields()
    {
        var (_, service, orgId) = CreateTestContext();
        var created = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Original", SortOrder = 0 });

        var result = await service.UpdateSuggestionAsync(orgId, created.Id,
            new UpdateSuggestionRequest { Value = "Updated", SortOrder = 10, IsActive = false });

        result.Should().NotBeNull();
        result!.Value.Should().Be("Updated");
        result.SortOrder.Should().Be(10);
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSuggestionAsync_DuplicateValue_ThrowsInvalidOperationException()
    {
        var (_, service, orgId) = CreateTestContext();
        await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Existing" });
        var toUpdate = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Original" });

        var act = () => service.UpdateSuggestionAsync(orgId, toUpdate.Id,
            new UpdateSuggestionRequest { Value = "existing", SortOrder = 0, IsActive = true });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateSuggestionAsync_SameValueDifferentCase_AllowsSelfUpdate()
    {
        var (_, service, orgId) = CreateTestContext();
        var created = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Alpha" });

        // Update same suggestion to different case — should be allowed
        var result = await service.UpdateSuggestionAsync(orgId, created.Id,
            new UpdateSuggestionRequest { Value = "ALPHA", SortOrder = 0, IsActive = true });

        result.Should().NotBeNull();
        result!.Value.Should().Be("ALPHA");
    }

    [Fact]
    public async Task UpdateSuggestionAsync_EmptyValue_ThrowsArgumentException()
    {
        var (_, service, orgId) = CreateTestContext();
        var created = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Test" });

        var act = () => service.UpdateSuggestionAsync(orgId, created.Id,
            new UpdateSuggestionRequest { Value = "  ", SortOrder = 0, IsActive = true });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // =========================================================================
    // DeleteSuggestionAsync
    // =========================================================================

    [Fact]
    public async Task DeleteSuggestionAsync_NonexistentId_ReturnsFalse()
    {
        var (_, service, orgId) = CreateTestContext();

        var result = await service.DeleteSuggestionAsync(orgId, Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSuggestionAsync_ExistingId_SoftDeletesAndReturnsTrue()
    {
        var (_, service, orgId) = CreateTestContext();
        var created = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "ToDelete" });

        var result = await service.DeleteSuggestionAsync(orgId, created.Id);

        result.Should().BeTrue();

        // Should no longer appear in active results
        var remaining = await service.GetSuggestionsAsync(orgId, SuggestionFieldNames.Track);
        remaining.Should().BeEmpty();
    }

    // =========================================================================
    // BulkCreateSuggestionsAsync
    // =========================================================================

    [Fact]
    public async Task BulkCreateSuggestionsAsync_InvalidFieldName_ThrowsArgumentException()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new BulkCreateSuggestionsRequest
        {
            FieldName = "BadField",
            Values = new List<string> { "A" }
        };

        var act = () => service.BulkCreateSuggestionsAsync(orgId, request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BulkCreateSuggestionsAsync_AllNew_CreatesAll()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new BulkCreateSuggestionsRequest
        {
            FieldName = SuggestionFieldNames.Track,
            Values = new List<string> { "Alpha", "Bravo", "Charlie" }
        };

        var result = await service.BulkCreateSuggestionsAsync(orgId, request);

        result.TotalProvided.Should().Be(3);
        result.Created.Should().Be(3);
        result.SkippedDuplicates.Should().Be(0);
    }

    [Fact]
    public async Task BulkCreateSuggestionsAsync_WithDuplicates_SkipsDuplicates()
    {
        var (_, service, orgId) = CreateTestContext();
        await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Alpha" });

        var request = new BulkCreateSuggestionsRequest
        {
            FieldName = SuggestionFieldNames.Track,
            Values = new List<string> { "Alpha", "Bravo" }
        };

        var result = await service.BulkCreateSuggestionsAsync(orgId, request);

        result.Created.Should().Be(1);
        result.SkippedDuplicates.Should().Be(1);
    }

    [Fact]
    public async Task BulkCreateSuggestionsAsync_DeduplicatesInput()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new BulkCreateSuggestionsRequest
        {
            FieldName = SuggestionFieldNames.Track,
            Values = new List<string> { "Alpha", "alpha", "ALPHA" }
        };

        var result = await service.BulkCreateSuggestionsAsync(orgId, request);

        result.Created.Should().Be(1);
    }

    [Fact]
    public async Task BulkCreateSuggestionsAsync_EmptyValues_ReturnsAllSkipped()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new BulkCreateSuggestionsRequest
        {
            FieldName = SuggestionFieldNames.Track,
            Values = new List<string> { "  ", "", "   " }
        };

        var result = await service.BulkCreateSuggestionsAsync(orgId, request);

        result.Created.Should().Be(0);
        result.SkippedDuplicates.Should().Be(3);
    }

    [Fact]
    public async Task BulkCreateSuggestionsAsync_AssignsSequentialSortOrders()
    {
        var (_, service, orgId) = CreateTestContext();
        // Seed one with sort order 5
        await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Existing", SortOrder = 5 });

        var request = new BulkCreateSuggestionsRequest
        {
            FieldName = SuggestionFieldNames.Track,
            Values = new List<string> { "New A", "New B" }
        };

        await service.BulkCreateSuggestionsAsync(orgId, request);

        var all = (await service.GetSuggestionsAsync(orgId, SuggestionFieldNames.Track)).ToList();
        var newA = all.First(s => s.Value == "New A");
        var newB = all.First(s => s.Value == "New B");

        // Should be after existing sort order 5
        newA.SortOrder.Should().Be(6);
        newB.SortOrder.Should().Be(7);
    }

    // =========================================================================
    // ReorderSuggestionsAsync
    // =========================================================================

    [Fact]
    public async Task ReorderSuggestionsAsync_InvalidFieldName_ThrowsArgumentException()
    {
        var (_, service, orgId) = CreateTestContext();

        var act = () => service.ReorderSuggestionsAsync(orgId, "BadField", new List<Guid>());

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReorderSuggestionsAsync_ValidReorder_UpdatesSortOrders()
    {
        var (_, service, orgId) = CreateTestContext();
        var a = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "A", SortOrder = 0 });
        var b = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "B", SortOrder = 1 });

        // Reverse
        await service.ReorderSuggestionsAsync(orgId, SuggestionFieldNames.Track,
            new List<Guid> { b.Id, a.Id });

        var result = (await service.GetSuggestionsAsync(orgId, SuggestionFieldNames.Track)).ToList();
        result[0].Value.Should().Be("B");
        result[1].Value.Should().Be("A");
    }

    // =========================================================================
    // BlockValueAsync
    // =========================================================================

    [Fact]
    public async Task BlockValueAsync_InvalidFieldName_ThrowsArgumentException()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new BlockSuggestionRequest { FieldName = "BadField", Value = "Test" };

        var act = () => service.BlockValueAsync(orgId, request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BlockValueAsync_EmptyValue_ThrowsArgumentException()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new BlockSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "  " };

        var act = () => service.BlockValueAsync(orgId, request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BlockValueAsync_NewValue_CreatesBlockedSuggestion()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new BlockSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Bad Track" };

        var result = await service.BlockValueAsync(orgId, request);

        result.IsBlocked.Should().BeTrue();
        result.IsActive.Should().BeFalse();
        result.Value.Should().Be("Bad Track");
    }

    [Fact]
    public async Task BlockValueAsync_AlreadyBlocked_ThrowsInvalidOperationException()
    {
        var (_, service, orgId) = CreateTestContext();
        var request = new BlockSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Bad Track" };
        await service.BlockValueAsync(orgId, request);

        var act = () => service.BlockValueAsync(orgId, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already blocked*");
    }

    [Fact]
    public async Task BlockValueAsync_ExistingCuratedValue_ThrowsInvalidOperationException()
    {
        var (_, service, orgId) = CreateTestContext();
        await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Good Track" });

        var act = () => service.BlockValueAsync(orgId,
            new BlockSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Good Track" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*curated*");
    }

    // =========================================================================
    // UnblockAsync
    // =========================================================================

    [Fact]
    public async Task UnblockAsync_NonexistentId_ReturnsFalse()
    {
        var (_, service, orgId) = CreateTestContext();

        var result = await service.UnblockAsync(orgId, Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnblockAsync_BlockedValue_SoftDeletesAndReturnsTrue()
    {
        var (_, service, orgId) = CreateTestContext();
        var blocked = await service.BlockValueAsync(orgId,
            new BlockSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Bad Track" });

        var result = await service.UnblockAsync(orgId, blocked.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnblockAsync_NonBlockedSuggestion_ReturnsFalse()
    {
        var (_, service, orgId) = CreateTestContext();
        var created = await service.CreateSuggestionAsync(orgId,
            new CreateSuggestionRequest { FieldName = SuggestionFieldNames.Track, Value = "Normal" });

        // Try to unblock a non-blocked suggestion
        var result = await service.UnblockAsync(orgId, created.Id);

        result.Should().BeFalse();
    }
}
