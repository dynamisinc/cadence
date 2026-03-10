using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Injects.Validators;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Injects.Validators;

/// <summary>
/// Tests for <see cref="CreateInjectRequestValidator"/>.
/// Each test constructs a request that differs from the valid baseline in exactly one field.
/// Properties use init-only setters, so each variation constructs a fresh object via the factory method.
/// </summary>
public class CreateInjectRequestValidatorTests
{
    private readonly CreateInjectRequestValidator _validator = new();

    // =========================================================================
    // Happy Path
    // =========================================================================

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidRequest_WithAllOptionalFieldsNull_PassesValidation()
    {
        // All nullable fields are null by default in ValidRequest().
        var result = _validator.Validate(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    // =========================================================================
    // Title
    // =========================================================================

    [Fact]
    public void Title_Empty_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(title: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Title));
    }

    [Fact]
    public void Title_Whitespace_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(title: "   "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Title));
    }

    [Fact]
    public void Title_TwoCharacters_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(title: "AB"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Title));
    }

    [Fact]
    public void Title_ExactlyMinLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(title: new string('x', CreateInjectRequestValidator.TitleMinLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Title));
    }

    [Fact]
    public void Title_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(title: new string('x', CreateInjectRequestValidator.TitleMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Title));
    }

    [Fact]
    public void Title_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(title: new string('x', CreateInjectRequestValidator.TitleMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Title));
    }

    // =========================================================================
    // Description
    // =========================================================================

    [Fact]
    public void Description_Empty_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(description: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Description));
    }

    [Fact]
    public void Description_Whitespace_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(description: "   "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Description));
    }

    [Fact]
    public void Description_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(description: new string('x', CreateInjectRequestValidator.DescriptionMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Description));
    }

    [Fact]
    public void Description_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(description: new string('x', CreateInjectRequestValidator.DescriptionMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Description));
    }

    // =========================================================================
    // Target
    // =========================================================================

    [Fact]
    public void Target_Empty_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(target: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Target));
    }

    [Fact]
    public void Target_Whitespace_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(target: "   "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Target));
    }

    [Fact]
    public void Target_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(target: new string('x', CreateInjectRequestValidator.TargetMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Target));
    }

    [Fact]
    public void Target_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(target: new string('x', CreateInjectRequestValidator.TargetMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Target));
    }

    // =========================================================================
    // Source (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void Source_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(source: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Source));
    }

    [Fact]
    public void Source_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(source: new string('x', CreateInjectRequestValidator.SourceMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Source));
    }

    [Fact]
    public void Source_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(source: new string('x', CreateInjectRequestValidator.SourceMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Source));
    }

    // =========================================================================
    // ExpectedAction (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void ExpectedAction_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(expectedAction: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ExpectedAction));
    }

    [Fact]
    public void ExpectedAction_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(expectedAction: new string('x', CreateInjectRequestValidator.ExpectedActionMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ExpectedAction));
    }

    [Fact]
    public void ExpectedAction_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(expectedAction: new string('x', CreateInjectRequestValidator.ExpectedActionMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.ExpectedAction));
    }

    // =========================================================================
    // ControllerNotes (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void ControllerNotes_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(controllerNotes: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ControllerNotes));
    }

    [Fact]
    public void ControllerNotes_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(controllerNotes: new string('x', CreateInjectRequestValidator.ControllerNotesMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ControllerNotes));
    }

    [Fact]
    public void ControllerNotes_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(controllerNotes: new string('x', CreateInjectRequestValidator.ControllerNotesMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.ControllerNotes));
    }

    // =========================================================================
    // TriggerCondition (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void TriggerCondition_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(triggerCondition: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.TriggerCondition));
    }

    [Fact]
    public void TriggerCondition_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(triggerCondition: new string('x', CreateInjectRequestValidator.TriggerConditionMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.TriggerCondition));
    }

    [Fact]
    public void TriggerCondition_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(triggerCondition: new string('x', CreateInjectRequestValidator.TriggerConditionMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.TriggerCondition));
    }

    // =========================================================================
    // SourceReference (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void SourceReference_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(sourceReference: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.SourceReference));
    }

    [Fact]
    public void SourceReference_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(sourceReference: new string('x', CreateInjectRequestValidator.SourceReferenceMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.SourceReference));
    }

    [Fact]
    public void SourceReference_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(sourceReference: new string('x', CreateInjectRequestValidator.SourceReferenceMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.SourceReference));
    }

    // =========================================================================
    // Priority (optional — 1-5 enforced only when non-null)
    // =========================================================================

    [Fact]
    public void Priority_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(priority: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Priority));
    }

    [Fact]
    public void Priority_MinValue_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(priority: CreateInjectRequestValidator.PriorityMin));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Priority));
    }

    [Fact]
    public void Priority_MaxValue_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(priority: CreateInjectRequestValidator.PriorityMax));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Priority));
    }

    [Fact]
    public void Priority_BelowMin_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(priority: CreateInjectRequestValidator.PriorityMin - 1));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Priority));
    }

    [Fact]
    public void Priority_AboveMax_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(priority: CreateInjectRequestValidator.PriorityMax + 1));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Priority));
    }

    // =========================================================================
    // InjectType (enum validation)
    // =========================================================================

    [Fact]
    public void InjectType_Standard_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(injectType: InjectType.Standard));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.InjectType));
    }

    [Fact]
    public void InjectType_Contingency_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(injectType: InjectType.Contingency));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.InjectType));
    }

    [Fact]
    public void InjectType_InvalidEnumValue_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(injectType: (InjectType)999));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.InjectType));
    }

    // =========================================================================
    // TriggerType (enum validation)
    // =========================================================================

    [Fact]
    public void TriggerType_Manual_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(triggerType: TriggerType.Manual));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.TriggerType));
    }

    [Fact]
    public void TriggerType_Scheduled_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(triggerType: TriggerType.Scheduled));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.TriggerType));
    }

    [Fact]
    public void TriggerType_InvalidEnumValue_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(triggerType: (TriggerType)999));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.TriggerType));
    }

    // =========================================================================
    // ResponsibleController (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void ResponsibleController_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(responsibleController: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ResponsibleController));
    }

    [Fact]
    public void ResponsibleController_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(responsibleController: new string('x', CreateInjectRequestValidator.ResponsibleControllerMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ResponsibleController));
    }

    [Fact]
    public void ResponsibleController_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(responsibleController: new string('x', CreateInjectRequestValidator.ResponsibleControllerMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.ResponsibleController));
    }

    // =========================================================================
    // LocationName (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void LocationName_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(locationName: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.LocationName));
    }

    [Fact]
    public void LocationName_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(locationName: new string('x', CreateInjectRequestValidator.LocationNameMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.LocationName));
    }

    [Fact]
    public void LocationName_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(locationName: new string('x', CreateInjectRequestValidator.LocationNameMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.LocationName));
    }

    // =========================================================================
    // LocationType (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void LocationType_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(locationType: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.LocationType));
    }

    [Fact]
    public void LocationType_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(locationType: new string('x', CreateInjectRequestValidator.LocationTypeMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.LocationType));
    }

    [Fact]
    public void LocationType_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(locationType: new string('x', CreateInjectRequestValidator.LocationTypeMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.LocationType));
    }

    // =========================================================================
    // Track (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void Track_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(track: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Track));
    }

    [Fact]
    public void Track_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(track: new string('x', CreateInjectRequestValidator.TrackMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.Track));
    }

    [Fact]
    public void Track_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(track: new string('x', CreateInjectRequestValidator.TrackMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.Track));
    }

    // =========================================================================
    // DeliveryMethodOther (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void DeliveryMethodOther_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(deliveryMethodOther: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.DeliveryMethodOther));
    }

    [Fact]
    public void DeliveryMethodOther_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(deliveryMethodOther: new string('x', CreateInjectRequestValidator.DeliveryMethodOtherMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.DeliveryMethodOther));
    }

    [Fact]
    public void DeliveryMethodOther_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(deliveryMethodOther: new string('x', CreateInjectRequestValidator.DeliveryMethodOtherMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.DeliveryMethodOther));
    }

    // =========================================================================
    // ScenarioDay (optional — 1-99 enforced only when non-null)
    // =========================================================================

    [Fact]
    public void ScenarioDay_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(scenarioDay: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_MinValue_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(scenarioDay: 1));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_MaxValue_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(scenarioDay: 99));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_Zero_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(scenarioDay: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_OneHundred_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(scenarioDay: 100));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.ScenarioDay));
    }

    // =========================================================================
    // ScenarioDay cross-field: required when ScenarioTime is set
    // =========================================================================

    [Fact]
    public void ScenarioDay_NullWhenScenarioTimeSet_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(scenarioDay: null, scenarioTime: new TimeOnly(8, 0)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_SetWhenScenarioTimeSet_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(scenarioDay: 1, scenarioTime: new TimeOnly(8, 0)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_NullWhenScenarioTimeAlsoNull_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(scenarioDay: null, scenarioTime: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInjectRequest.ScenarioDay));
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Returns a valid <see cref="CreateInjectRequest"/> with optional per-field overrides.
    /// All parameters default to baseline valid values.
    /// </summary>
    private static CreateInjectRequest ValidRequest(
        string title = "Activate EOC Level 2",
        string description = "Notify the EOC Director to activate Level 2 operations immediately.",
        string target = "EOC Director",
        string? source = null,
        string? expectedAction = null,
        string? controllerNotes = null,
        string? triggerCondition = null,
        string? sourceReference = null,
        int? priority = null,
        InjectType injectType = InjectType.Standard,
        TriggerType triggerType = TriggerType.Manual,
        string? responsibleController = null,
        string? locationName = null,
        string? locationType = null,
        string? track = null,
        string? deliveryMethodOther = null,
        int? scenarioDay = null,
        TimeOnly? scenarioTime = null) => new()
    {
        Title = title,
        Description = description,
        ScheduledTime = new TimeOnly(9, 0),
        Target = target,
        Source = source,
        ExpectedAction = expectedAction,
        ControllerNotes = controllerNotes,
        TriggerCondition = triggerCondition,
        SourceReference = sourceReference,
        Priority = priority,
        InjectType = injectType,
        TriggerType = triggerType,
        ResponsibleController = responsibleController,
        LocationName = locationName,
        LocationType = locationType,
        Track = track,
        DeliveryMethodOther = deliveryMethodOther,
        ScenarioDay = scenarioDay,
        ScenarioTime = scenarioTime
    };
}
