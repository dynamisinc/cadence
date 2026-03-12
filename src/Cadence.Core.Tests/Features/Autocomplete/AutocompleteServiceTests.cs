using Cadence.Core.Data;
using Cadence.Core.Features.Autocomplete.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Autocomplete;

/// <summary>
/// Tests for AutocompleteService — org-scoped autocomplete merging managed suggestions with historical inject data.
/// </summary>
public class AutocompleteServiceTests
{
    private (AppDbContext context, AutocompleteService service, Organization org) CreateTestContext()
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

        var service = new AutocompleteService(context);
        return (context, service, org);
    }

    private (Exercise exercise, Msel msel) CreateExerciseWithMsel(AppDbContext context, Guid orgId)
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            OrganizationId = orgId,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        context.Exercises.Add(exercise);

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            ExerciseId = exercise.Id,
            OrganizationId = orgId
        };
        context.Msels.Add(msel);
        context.SaveChanges();

        return (exercise, msel);
    }

    private void AddInjects(AppDbContext context, Guid mselId, string fieldName, params string?[] values)
    {
        var counter = 1;
        foreach (var value in values)
        {
            var inject = new Inject
            {
                Id = Guid.NewGuid(),
                MselId = mselId,
                Title = $"Inject {counter}",
                Description = "Description",
                Target = "EOC",
                InjectNumber = counter++
            };

            // Set the appropriate field
            switch (fieldName)
            {
                case SuggestionFieldNames.Track: inject.Track = value; break;
                case SuggestionFieldNames.Target: inject.Target = value ?? "EOC"; break;
                case SuggestionFieldNames.Source: inject.Source = value; break;
                case SuggestionFieldNames.LocationName: inject.LocationName = value; break;
                case SuggestionFieldNames.LocationType: inject.LocationType = value; break;
                case SuggestionFieldNames.ResponsibleController: inject.ResponsibleController = value; break;
            }

            context.Injects.Add(inject);
        }
        context.SaveChanges();
    }

    private void AddManagedSuggestion(AppDbContext context, Guid orgId, string fieldName, string value,
        int sortOrder = 0, bool isActive = true, bool isBlocked = false)
    {
        context.OrganizationSuggestions.Add(new OrganizationSuggestion
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            FieldName = fieldName,
            Value = value,
            SortOrder = sortOrder,
            IsActive = isActive,
            IsBlocked = isBlocked
        });
        context.SaveChanges();
    }

    // =========================================================================
    // GetExerciseOrganizationIdAsync
    // =========================================================================

    [Fact]
    public async Task GetExerciseOrganizationIdAsync_ExistingExercise_ReturnsOrgId()
    {
        var (context, service, org) = CreateTestContext();
        var (exercise, _) = CreateExerciseWithMsel(context, org.Id);

        var result = await service.GetExerciseOrganizationIdAsync(exercise.Id);

        result.Should().Be(org.Id);
    }

    [Fact]
    public async Task GetExerciseOrganizationIdAsync_NonexistentExercise_ReturnsNull()
    {
        var (_, service, _) = CreateTestContext();

        var result = await service.GetExerciseOrganizationIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // =========================================================================
    // GetTrackSuggestionsAsync — managed suggestions first, then historical
    // =========================================================================

    [Fact]
    public async Task GetTrackSuggestionsAsync_ManagedFirst_ThenHistorical()
    {
        var (context, service, org) = CreateTestContext();
        var (_, msel) = CreateExerciseWithMsel(context, org.Id);

        AddManagedSuggestion(context, org.Id, SuggestionFieldNames.Track, "Alpha Track", sortOrder: 1);
        AddInjects(context, msel.Id, SuggestionFieldNames.Track, "Beta Track", "Gamma Track");

        var result = await service.GetTrackSuggestionsAsync(org.Id);

        result.Should().HaveCountGreaterOrEqualTo(2);
        result[0].Should().Be("Alpha Track"); // Managed first
    }

    [Fact]
    public async Task GetTrackSuggestionsAsync_DeduplicatesHistoricalAgainstManaged()
    {
        var (context, service, org) = CreateTestContext();
        var (_, msel) = CreateExerciseWithMsel(context, org.Id);

        AddManagedSuggestion(context, org.Id, SuggestionFieldNames.Track, "Alpha Track");
        AddInjects(context, msel.Id, SuggestionFieldNames.Track, "Alpha Track", "Alpha Track", "Beta Track");

        var result = await service.GetTrackSuggestionsAsync(org.Id);

        result.Should().Contain("Alpha Track");
        result.Should().Contain("Beta Track");
        // Alpha should appear only once (from managed)
        result.Count(v => v == "Alpha Track").Should().Be(1);
    }

    [Fact]
    public async Task GetTrackSuggestionsAsync_WithFilter_ReturnsMatchingOnly()
    {
        var (context, service, org) = CreateTestContext();
        var (_, msel) = CreateExerciseWithMsel(context, org.Id);

        AddInjects(context, msel.Id, SuggestionFieldNames.Track, "Fire Track", "Police Track", "EMS Track");

        var result = await service.GetTrackSuggestionsAsync(org.Id, filter: "fire");

        result.Should().Contain("Fire Track");
        result.Should().NotContain("Police Track");
    }

    [Fact]
    public async Task GetTrackSuggestionsAsync_RespectsLimit()
    {
        var (context, service, org) = CreateTestContext();
        var (_, msel) = CreateExerciseWithMsel(context, org.Id);

        for (int i = 0; i < 25; i++)
            AddInjects(context, msel.Id, SuggestionFieldNames.Track, $"Track {i:D2}");

        var result = await service.GetTrackSuggestionsAsync(org.Id, limit: 5);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetTrackSuggestionsAsync_InactiveManagedSuggestions_NotReturned()
    {
        var (context, service, org) = CreateTestContext();
        AddManagedSuggestion(context, org.Id, SuggestionFieldNames.Track, "Inactive Track", isActive: false);

        var result = await service.GetTrackSuggestionsAsync(org.Id);

        result.Should().NotContain("Inactive Track");
    }

    [Fact]
    public async Task GetTrackSuggestionsAsync_BlockedHistoricalValues_Suppressed()
    {
        var (context, service, org) = CreateTestContext();
        var (_, msel) = CreateExerciseWithMsel(context, org.Id);

        AddInjects(context, msel.Id, SuggestionFieldNames.Track, "Bad Track", "Good Track");
        AddManagedSuggestion(context, org.Id, SuggestionFieldNames.Track, "Bad Track", isActive: false, isBlocked: true);

        var result = await service.GetTrackSuggestionsAsync(org.Id);

        result.Should().NotContain("Bad Track");
        result.Should().Contain("Good Track");
    }

    // =========================================================================
    // Other field-specific methods delegate to the same core logic
    // =========================================================================

    [Fact]
    public async Task GetTargetSuggestionsAsync_ReturnsSuggestions()
    {
        var (context, service, org) = CreateTestContext();
        AddManagedSuggestion(context, org.Id, SuggestionFieldNames.Target, "EOC Team");

        var result = await service.GetTargetSuggestionsAsync(org.Id);

        result.Should().Contain("EOC Team");
    }

    [Fact]
    public async Task GetSourceSuggestionsAsync_ReturnsSuggestions()
    {
        var (context, service, org) = CreateTestContext();
        AddManagedSuggestion(context, org.Id, SuggestionFieldNames.Source, "Dispatch Center");

        var result = await service.GetSourceSuggestionsAsync(org.Id);

        result.Should().Contain("Dispatch Center");
    }

    [Fact]
    public async Task GetLocationNameSuggestionsAsync_ReturnsSuggestions()
    {
        var (context, service, org) = CreateTestContext();
        AddManagedSuggestion(context, org.Id, SuggestionFieldNames.LocationName, "Main EOC");

        var result = await service.GetLocationNameSuggestionsAsync(org.Id);

        result.Should().Contain("Main EOC");
    }

    [Fact]
    public async Task GetLocationTypeSuggestionsAsync_ReturnsSuggestions()
    {
        var (context, service, org) = CreateTestContext();
        AddManagedSuggestion(context, org.Id, SuggestionFieldNames.LocationType, "Indoor");

        var result = await service.GetLocationTypeSuggestionsAsync(org.Id);

        result.Should().Contain("Indoor");
    }

    [Fact]
    public async Task GetResponsibleControllerSuggestionsAsync_ReturnsSuggestions()
    {
        var (context, service, org) = CreateTestContext();
        AddManagedSuggestion(context, org.Id, SuggestionFieldNames.ResponsibleController, "John Smith");

        var result = await service.GetResponsibleControllerSuggestionsAsync(org.Id);

        result.Should().Contain("John Smith");
    }

    // =========================================================================
    // GetHistoricalValuesAsync
    // =========================================================================

    [Fact]
    public async Task GetHistoricalValuesAsync_ReturnsUncuratedValues()
    {
        var (context, service, org) = CreateTestContext();
        var (_, msel) = CreateExerciseWithMsel(context, org.Id);

        AddInjects(context, msel.Id, SuggestionFieldNames.Track, "Track A", "Track B");
        AddManagedSuggestion(context, org.Id, SuggestionFieldNames.Track, "Track A");

        var result = await service.GetHistoricalValuesAsync(org.Id, SuggestionFieldNames.Track);

        // Track A is curated, so should be excluded from historical
        result.Should().Contain("Track B");
        result.Should().NotContain("Track A");
    }

    [Fact]
    public async Task GetHistoricalValuesAsync_InvalidFieldName_ThrowsArgumentException()
    {
        var (_, service, org) = CreateTestContext();

        var act = () => service.GetHistoricalValuesAsync(org.Id, "InvalidField");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetHistoricalValuesAsync_NoExercises_ReturnsEmpty()
    {
        var (_, service, org) = CreateTestContext();

        var result = await service.GetHistoricalValuesAsync(org.Id, SuggestionFieldNames.Track);

        result.Should().BeEmpty();
    }

    // =========================================================================
    // Historical data ordered by frequency
    // =========================================================================

    [Fact]
    public async Task GetTrackSuggestionsAsync_HistoricalOrderedByFrequency()
    {
        var (context, service, org) = CreateTestContext();
        var (_, msel) = CreateExerciseWithMsel(context, org.Id);

        // "Frequent Track" appears 3 times, "Rare Track" once
        AddInjects(context, msel.Id, SuggestionFieldNames.Track,
            "Frequent Track", "Frequent Track", "Frequent Track", "Rare Track");

        var result = await service.GetTrackSuggestionsAsync(org.Id);

        var freqIndex = result.IndexOf("Frequent Track");
        var rareIndex = result.IndexOf("Rare Track");
        freqIndex.Should().BeLessThan(rareIndex);
    }

    // =========================================================================
    // Cross-organization isolation
    // =========================================================================

    [Fact]
    public async Task GetTrackSuggestionsAsync_IsolatedByOrganization()
    {
        var (context, service, org) = CreateTestContext();

        var otherOrg = new Organization { Id = Guid.NewGuid(), Name = "Other Org", Slug = "other-org" };
        context.Organizations.Add(otherOrg);
        context.SaveChanges();

        var (_, otherMsel) = CreateExerciseWithMsel(context, otherOrg.Id);
        AddInjects(context, otherMsel.Id, SuggestionFieldNames.Track, "Other Org Track");

        var result = await service.GetTrackSuggestionsAsync(org.Id);

        result.Should().NotContain("Other Org Track");
    }
}
