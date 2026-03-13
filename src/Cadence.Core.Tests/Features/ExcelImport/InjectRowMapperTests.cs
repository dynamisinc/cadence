using Cadence.Core.Features.ExcelImport.Models.DTOs;
using Cadence.Core.Features.ExcelImport.Services;
using Cadence.Core.Models.Entities;
using FluentAssertions;
using Xunit;

namespace Cadence.Core.Tests.Features.ExcelImport;

/// <summary>
/// Unit tests for <see cref="InjectRowMapper"/>.
/// Each test covers one field mapping case from the 18-way switch statement.
/// </summary>
public class InjectRowMapperTests
{
    private readonly Guid _exerciseId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();

    private static ColumnMappingDto Mapping(string field) => new()
    {
        CadenceField = field,
        SourceColumnIndex = 0,
        DisplayName = field,
        IsRequired = false
    };

    private static Dictionary<string, object?> Values(string field, object? value) =>
        new() { [field] = value };

    private MappingResult Map(
        string field,
        object? value,
        Inject? inject = null,
        Dictionary<string, Phase>? phases = null,
        Dictionary<string, DeliveryMethodLookup>? deliveryMethods = null,
        bool createMissingPhases = false)
    {
        inject ??= new Inject();
        phases ??= new Dictionary<string, Phase>();
        deliveryMethods ??= new Dictionary<string, DeliveryMethodLookup>();

        return InjectRowMapper.MapRowToInject(
            inject,
            Values(field, value),
            new[] { Mapping(field) },
            phases,
            deliveryMethods,
            _exerciseId,
            _orgId,
            createMissingPhases);
    }

    // =========================================================================
    // Simple string fields
    // =========================================================================

    [Fact]
    public void MapRow_InjectNumber_SetsSourceReference()
    {
        var inject = new Inject();
        Map("InjectNumber", "INJ-42", inject);
        inject.SourceReference.Should().Be("INJ-42");
    }

    [Fact]
    public void MapRow_Title_SetsTitle()
    {
        var inject = new Inject();
        Map("Title", "Power Outage", inject);
        inject.Title.Should().Be("Power Outage");
    }

    [Fact]
    public void MapRow_Description_SetsDescription()
    {
        var inject = new Inject();
        Map("Description", "A major power outage occurs", inject);
        inject.Description.Should().Be("A major power outage occurs");
    }

    [Fact]
    public void MapRow_Source_SetsSource()
    {
        var inject = new Inject();
        Map("Source", "EOC", inject);
        inject.Source.Should().Be("EOC");
    }

    [Fact]
    public void MapRow_Target_SetsTarget()
    {
        var inject = new Inject();
        Map("Target", "Fire Department", inject);
        inject.Target.Should().Be("Fire Department");
    }

    [Fact]
    public void MapRow_Track_SetsTrack()
    {
        var inject = new Inject();
        Map("Track", "Operations", inject);
        inject.Track.Should().Be("Operations");
    }

    [Fact]
    public void MapRow_ExpectedAction_SetsExpectedAction()
    {
        var inject = new Inject();
        Map("ExpectedAction", "Dispatch unit", inject);
        inject.ExpectedAction.Should().Be("Dispatch unit");
    }

    [Fact]
    public void MapRow_Notes_SetsControllerNotes()
    {
        var inject = new Inject();
        Map("Notes", "Be ready for follow-up", inject);
        inject.ControllerNotes.Should().Be("Be ready for follow-up");
    }

    [Fact]
    public void MapRow_LocationName_SetsLocationName()
    {
        var inject = new Inject();
        Map("LocationName", "Building A", inject);
        inject.LocationName.Should().Be("Building A");
    }

    [Fact]
    public void MapRow_LocationType_SetsLocationType()
    {
        var inject = new Inject();
        Map("LocationType", "Indoor", inject);
        inject.LocationType.Should().Be("Indoor");
    }

    [Fact]
    public void MapRow_ResponsibleController_SetsResponsibleController()
    {
        var inject = new Inject();
        Map("ResponsibleController", "John Smith", inject);
        inject.ResponsibleController.Should().Be("John Smith");
    }

    // =========================================================================
    // Time fields
    // =========================================================================

    [Fact]
    public void MapRow_ScheduledTime_TimeOnly_SetsScheduledTime()
    {
        var inject = new Inject();
        Map("ScheduledTime", "14:30", inject);
        inject.ScheduledTime.Should().Be(new TimeOnly(14, 30));
    }

    [Fact]
    public void MapRow_ScheduledTime_DateTime_SetsScheduledTime()
    {
        var inject = new Inject();
        // DateTime value — TryParseTime handles DateTime directly, extracting time only
        Map("ScheduledTime", new DateTime(2026, 3, 15, 10, 0, 0), inject);
        inject.ScheduledTime.Should().Be(new TimeOnly(10, 0));
    }

    [Fact]
    public void MapRow_ScheduledTime_ExcelSerial_SetsScheduledTimeAndScenarioDay()
    {
        var inject = new Inject();
        // Excel date serial >= 1.0 — TryParseTime fails (returns false for d >= 1.0),
        // TryParseDateTime succeeds, extracting both time and scenario day
        // 46101.4375 = 2026-03-20 10:30:00 in OADate serial format
        Map("ScheduledTime", 46101.4375, inject);
        inject.ScheduledTime.Should().Be(new TimeOnly(10, 30));
        inject.ScenarioDay.Should().Be(20);
    }

    [Fact]
    public void MapRow_ScenarioDay_SetsScenarioDay()
    {
        var inject = new Inject();
        Map("ScenarioDay", "3", inject);
        inject.ScenarioDay.Should().Be(3);
    }

    [Fact]
    public void MapRow_ScenarioTime_SetsScenarioTime()
    {
        var inject = new Inject();
        Map("ScenarioTime", "09:15", inject);
        inject.ScenarioTime.Should().Be(new TimeOnly(9, 15));
    }

    // =========================================================================
    // Priority (clamped 1-5)
    // =========================================================================

    [Theory]
    [InlineData("3", 3)]
    [InlineData("1", 1)]
    [InlineData("5", 5)]
    [InlineData("0", 1)]   // clamped up
    [InlineData("10", 5)]  // clamped down
    [InlineData("-1", 1)]  // clamped up
    public void MapRow_Priority_ClampedTo1Through5(string input, int expected)
    {
        var inject = new Inject();
        Map("Priority", input, inject);
        inject.Priority.Should().Be(expected);
    }

    // =========================================================================
    // DeliveryMethod
    // =========================================================================

    [Fact]
    public void MapRow_DeliveryMethod_ExactMatch_SetsDeliveryMethodId()
    {
        var methodId = Guid.NewGuid();
        var methods = new Dictionary<string, DeliveryMethodLookup>
        {
            ["radio"] = new() { Id = methodId, Name = "Radio" }
        };

        var inject = new Inject();
        Map("DeliveryMethod", "Radio", inject, deliveryMethods: methods);
        inject.DeliveryMethodId.Should().Be(methodId);
    }

    [Fact]
    public void MapRow_DeliveryMethod_Synonym_SetsDeliveryMethodId()
    {
        var verbalId = Guid.NewGuid();
        var methods = new Dictionary<string, DeliveryMethodLookup>
        {
            ["verbal"] = new() { Id = verbalId, Name = "Verbal" }
        };

        var inject = new Inject();
        // "in person" is a synonym for "Verbal" in ColumnMappingStrategy.DeliveryMethodSynonyms
        Map("DeliveryMethod", "in person", inject, deliveryMethods: methods);
        inject.DeliveryMethodId.Should().Be(verbalId);
    }

    [Fact]
    public void MapRow_DeliveryMethod_Unknown_SetsOtherMethodAndText()
    {
        var otherId = Guid.NewGuid();
        var methods = new Dictionary<string, DeliveryMethodLookup>
        {
            ["other"] = new() { Id = otherId, Name = "Other", IsOther = true }
        };

        var inject = new Inject();
        Map("DeliveryMethod", "Carrier Pigeon", inject, deliveryMethods: methods);
        inject.DeliveryMethodId.Should().Be(otherId);
        inject.DeliveryMethodOther.Should().Be("Carrier Pigeon");
    }

    // =========================================================================
    // Phase
    // =========================================================================

    [Fact]
    public void MapRow_Phase_ExistingPhase_SetsPhaseId()
    {
        var phaseId = Guid.NewGuid();
        var phases = new Dictionary<string, Phase>
        {
            ["initial response"] = new() { Id = phaseId, Name = "Initial Response" }
        };

        var inject = new Inject();
        Map("Phase", "Initial Response", inject, phases: phases);
        inject.PhaseId.Should().Be(phaseId);
    }

    [Fact]
    public void MapRow_Phase_CreateMissing_CreatesPhaseAndSetsId()
    {
        var phases = new Dictionary<string, Phase>();

        var inject = new Inject();
        var result = Map("Phase", "Recovery", inject, phases: phases, createMissingPhases: true);

        inject.PhaseId.Should().NotBeEmpty();
        result.NewPhases.Should().HaveCount(1);
        result.NewPhases[0].Name.Should().Be("Recovery");
        result.NewPhases[0].ExerciseId.Should().Be(_exerciseId);
        result.NewPhases[0].OrganizationId.Should().Be(_orgId);
        // Phase should also be added to the dictionary for subsequent rows
        phases.Should().ContainKey("recovery");
    }

    [Fact]
    public void MapRow_Phase_NoCreate_AddsWarning()
    {
        var inject = new Inject();
        var result = Map("Phase", "Nonexistent", inject, createMissingPhases: false);

        inject.PhaseId.Should().BeNull();
        result.Warnings.Should().ContainSingle(w => w.Contains("Nonexistent") && w.Contains("not found"));
    }

    // =========================================================================
    // InjectType
    // =========================================================================

    [Fact]
    public void MapRow_InjectType_KnownSynonym_SetsInjectType()
    {
        var inject = new Inject();
        Map("InjectType", "contingency", inject);
        inject.InjectType.Should().Be(InjectType.Contingency);
    }

    [Fact]
    public void MapRow_InjectType_TriggerTypeLike_DefaultsToStandardWithWarning()
    {
        var inject = new Inject();
        var result = Map("InjectType", "controller action", inject);
        inject.InjectType.Should().Be(InjectType.Standard);
        result.Warnings.Should().ContainSingle(w => w.Contains("looks like a trigger type"));
    }

    [Fact]
    public void MapRow_InjectType_DeliveryMethodLike_DefaultsToStandardWithWarning()
    {
        var inject = new Inject();
        var result = Map("InjectType", "radio", inject);
        inject.InjectType.Should().Be(InjectType.Standard);
        result.Warnings.Should().ContainSingle(w => w.Contains("looks like a delivery method"));
    }

    [Fact]
    public void MapRow_InjectType_Unknown_DefaultsToStandardWithWarning()
    {
        var inject = new Inject();
        var result = Map("InjectType", "totally_unknown_type", inject);
        inject.InjectType.Should().Be(InjectType.Standard);
        result.Warnings.Should().ContainSingle(w => w.Contains("Unrecognized inject type"));
    }

    // =========================================================================
    // TriggerType
    // =========================================================================

    [Fact]
    public void MapRow_TriggerType_KnownSynonym_SetsTriggerType()
    {
        var inject = new Inject();
        Map("TriggerType", "automatic", inject);
        inject.TriggerType.Should().Be(TriggerType.Scheduled);
    }

    [Fact]
    public void MapRow_TriggerType_Unknown_DefaultsToManualWithWarning()
    {
        var inject = new Inject();
        var result = Map("TriggerType", "some_unknown_trigger", inject);
        inject.TriggerType.Should().Be(TriggerType.Manual);
        result.Warnings.Should().ContainSingle(w => w.Contains("Unrecognized trigger type"));
    }

    // =========================================================================
    // Edge cases
    // =========================================================================

    [Fact]
    public void MapRow_EmptyValue_Skipped()
    {
        var inject = new Inject { Title = "Original" };
        Map("Title", "", inject);
        inject.Title.Should().Be("Original"); // empty values skipped, original preserved
    }

    [Fact]
    public void MapRow_NullValue_Skipped()
    {
        var inject = new Inject { Title = "Original" };
        Map("Title", null, inject);
        inject.Title.Should().Be("Original"); // null values skipped, original preserved
    }

    [Fact]
    public void MapRow_UnmappedColumn_Skipped()
    {
        var inject = new Inject { Title = "Original" };
        // SourceColumnIndex is null → unmapped
        var unmappedMapping = new ColumnMappingDto
        {
            CadenceField = "Title",
            SourceColumnIndex = null,
            DisplayName = "Title",
            IsRequired = false
        };

        var result = InjectRowMapper.MapRowToInject(
            inject,
            new Dictionary<string, object?> { ["Title"] = "Should not be set" },
            new[] { unmappedMapping },
            new Dictionary<string, Phase>(),
            new Dictionary<string, DeliveryMethodLookup>(),
            _exerciseId,
            _orgId,
            false);

        inject.Title.Should().Be("Original"); // unmapped column skipped
        result.NewPhases.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void MapRow_MultipleFields_AllApplied()
    {
        var inject = new Inject();
        var mappings = new[]
        {
            Mapping("Title"),
            Mapping("Description"),
            Mapping("Source")
        };
        var values = new Dictionary<string, object?>
        {
            ["Title"] = "Test",
            ["Description"] = "Test desc",
            ["Source"] = "EOC"
        };

        InjectRowMapper.MapRowToInject(
            inject, values, mappings,
            new Dictionary<string, Phase>(),
            new Dictionary<string, DeliveryMethodLookup>(),
            _exerciseId, _orgId, false);

        inject.Title.Should().Be("Test");
        inject.Description.Should().Be("Test desc");
        inject.Source.Should().Be("EOC");
    }
}
