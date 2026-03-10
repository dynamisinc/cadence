using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Injects.Validators;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Injects.Validators;

/// <summary>
/// Tests for <see cref="UpdateInjectRequestValidator"/>.
/// Mirrors <see cref="CreateInjectRequestValidatorTests"/> using <see cref="UpdateInjectRequest"/>.
/// Each test constructs a request that differs from the valid baseline in exactly one field.
/// </summary>
public class UpdateInjectRequestValidatorTests
{
    private readonly UpdateInjectRequestValidator _validator = new();

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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Title));
    }

    [Fact]
    public void Title_Whitespace_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(title: "   "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Title));
    }

    [Fact]
    public void Title_TwoCharacters_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(title: "AB"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Title));
    }

    [Fact]
    public void Title_ExactlyMinLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(title: new string('x', UpdateInjectRequestValidator.TitleMinLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Title));
    }

    [Fact]
    public void Title_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(title: new string('x', UpdateInjectRequestValidator.TitleMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Title));
    }

    [Fact]
    public void Title_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(title: new string('x', UpdateInjectRequestValidator.TitleMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Title));
    }

    // =========================================================================
    // Description
    // =========================================================================

    [Fact]
    public void Description_Empty_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(description: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Description));
    }

    [Fact]
    public void Description_Whitespace_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(description: "   "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Description));
    }

    [Fact]
    public void Description_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(description: new string('x', UpdateInjectRequestValidator.DescriptionMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Description));
    }

    [Fact]
    public void Description_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(description: new string('x', UpdateInjectRequestValidator.DescriptionMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Description));
    }

    // =========================================================================
    // Target
    // =========================================================================

    [Fact]
    public void Target_Empty_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(target: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Target));
    }

    [Fact]
    public void Target_Whitespace_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(target: "   "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Target));
    }

    [Fact]
    public void Target_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(target: new string('x', UpdateInjectRequestValidator.TargetMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Target));
    }

    [Fact]
    public void Target_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(target: new string('x', UpdateInjectRequestValidator.TargetMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Target));
    }

    // =========================================================================
    // Source (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void Source_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(source: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Source));
    }

    [Fact]
    public void Source_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(source: new string('x', UpdateInjectRequestValidator.SourceMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Source));
    }

    [Fact]
    public void Source_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(source: new string('x', UpdateInjectRequestValidator.SourceMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Source));
    }

    // =========================================================================
    // ExpectedAction (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void ExpectedAction_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(expectedAction: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ExpectedAction));
    }

    [Fact]
    public void ExpectedAction_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(expectedAction: new string('x', UpdateInjectRequestValidator.ExpectedActionMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ExpectedAction));
    }

    [Fact]
    public void ExpectedAction_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(expectedAction: new string('x', UpdateInjectRequestValidator.ExpectedActionMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.ExpectedAction));
    }

    // =========================================================================
    // ControllerNotes (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void ControllerNotes_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(controllerNotes: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ControllerNotes));
    }

    [Fact]
    public void ControllerNotes_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(controllerNotes: new string('x', UpdateInjectRequestValidator.ControllerNotesMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ControllerNotes));
    }

    [Fact]
    public void ControllerNotes_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(controllerNotes: new string('x', UpdateInjectRequestValidator.ControllerNotesMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.ControllerNotes));
    }

    // =========================================================================
    // TriggerCondition (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void TriggerCondition_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(triggerCondition: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.TriggerCondition));
    }

    [Fact]
    public void TriggerCondition_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(triggerCondition: new string('x', UpdateInjectRequestValidator.TriggerConditionMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.TriggerCondition));
    }

    [Fact]
    public void TriggerCondition_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(triggerCondition: new string('x', UpdateInjectRequestValidator.TriggerConditionMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.TriggerCondition));
    }

    // =========================================================================
    // SourceReference (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void SourceReference_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(sourceReference: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.SourceReference));
    }

    [Fact]
    public void SourceReference_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(sourceReference: new string('x', UpdateInjectRequestValidator.SourceReferenceMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.SourceReference));
    }

    [Fact]
    public void SourceReference_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(sourceReference: new string('x', UpdateInjectRequestValidator.SourceReferenceMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.SourceReference));
    }

    // =========================================================================
    // Priority (optional — 1-5 enforced only when non-null)
    // =========================================================================

    [Fact]
    public void Priority_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(priority: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Priority));
    }

    [Fact]
    public void Priority_MinValue_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(priority: UpdateInjectRequestValidator.PriorityMin));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Priority));
    }

    [Fact]
    public void Priority_MaxValue_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(priority: UpdateInjectRequestValidator.PriorityMax));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Priority));
    }

    [Fact]
    public void Priority_BelowMin_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(priority: UpdateInjectRequestValidator.PriorityMin - 1));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Priority));
    }

    [Fact]
    public void Priority_AboveMax_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(priority: UpdateInjectRequestValidator.PriorityMax + 1));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Priority));
    }

    // =========================================================================
    // InjectType (enum validation)
    // =========================================================================

    [Fact]
    public void InjectType_Standard_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(injectType: InjectType.Standard));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.InjectType));
    }

    [Fact]
    public void InjectType_Adaptive_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(injectType: InjectType.Adaptive));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.InjectType));
    }

    [Fact]
    public void InjectType_InvalidEnumValue_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(injectType: (InjectType)999));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.InjectType));
    }

    // =========================================================================
    // TriggerType (enum validation)
    // =========================================================================

    [Fact]
    public void TriggerType_Manual_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(triggerType: TriggerType.Manual));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.TriggerType));
    }

    [Fact]
    public void TriggerType_Conditional_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(triggerType: TriggerType.Conditional));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.TriggerType));
    }

    [Fact]
    public void TriggerType_InvalidEnumValue_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(triggerType: (TriggerType)999));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.TriggerType));
    }

    // =========================================================================
    // ResponsibleController (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void ResponsibleController_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(responsibleController: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ResponsibleController));
    }

    [Fact]
    public void ResponsibleController_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(responsibleController: new string('x', UpdateInjectRequestValidator.ResponsibleControllerMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ResponsibleController));
    }

    [Fact]
    public void ResponsibleController_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(responsibleController: new string('x', UpdateInjectRequestValidator.ResponsibleControllerMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.ResponsibleController));
    }

    // =========================================================================
    // LocationName (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void LocationName_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(locationName: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.LocationName));
    }

    [Fact]
    public void LocationName_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(locationName: new string('x', UpdateInjectRequestValidator.LocationNameMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.LocationName));
    }

    [Fact]
    public void LocationName_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(locationName: new string('x', UpdateInjectRequestValidator.LocationNameMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.LocationName));
    }

    // =========================================================================
    // LocationType (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void LocationType_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(locationType: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.LocationType));
    }

    [Fact]
    public void LocationType_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(locationType: new string('x', UpdateInjectRequestValidator.LocationTypeMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.LocationType));
    }

    [Fact]
    public void LocationType_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(locationType: new string('x', UpdateInjectRequestValidator.LocationTypeMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.LocationType));
    }

    // =========================================================================
    // Track (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void Track_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(track: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Track));
    }

    [Fact]
    public void Track_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(track: new string('x', UpdateInjectRequestValidator.TrackMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.Track));
    }

    [Fact]
    public void Track_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(track: new string('x', UpdateInjectRequestValidator.TrackMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.Track));
    }

    // =========================================================================
    // DeliveryMethodOther (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void DeliveryMethodOther_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(deliveryMethodOther: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.DeliveryMethodOther));
    }

    [Fact]
    public void DeliveryMethodOther_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(deliveryMethodOther: new string('x', UpdateInjectRequestValidator.DeliveryMethodOtherMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.DeliveryMethodOther));
    }

    [Fact]
    public void DeliveryMethodOther_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(deliveryMethodOther: new string('x', UpdateInjectRequestValidator.DeliveryMethodOtherMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.DeliveryMethodOther));
    }

    // =========================================================================
    // ScenarioDay (optional — 1-99 enforced only when non-null)
    // =========================================================================

    [Fact]
    public void ScenarioDay_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(scenarioDay: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_MinValue_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(scenarioDay: 1));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_MaxValue_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(scenarioDay: 99));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_Zero_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(scenarioDay: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_OneHundred_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(scenarioDay: 100));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.ScenarioDay));
    }

    // =========================================================================
    // ScenarioDay cross-field: required when ScenarioTime is set
    // =========================================================================

    [Fact]
    public void ScenarioDay_NullWhenScenarioTimeSet_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(scenarioDay: null, scenarioTime: new TimeOnly(10, 30)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_SetWhenScenarioTimeSet_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(scenarioDay: 2, scenarioTime: new TimeOnly(10, 30)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ScenarioDay));
    }

    [Fact]
    public void ScenarioDay_NullWhenScenarioTimeAlsoNull_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(scenarioDay: null, scenarioTime: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInjectRequest.ScenarioDay));
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Returns a valid <see cref="UpdateInjectRequest"/> with optional per-field overrides.
    /// All parameters default to baseline valid values.
    /// </summary>
    private static UpdateInjectRequest ValidRequest(
        string title = "Request mutual aid from neighboring county",
        string description = "EOC Director contacts neighboring county EOC to request mutual aid activation.",
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
        ScheduledTime = new TimeOnly(11, 0),
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
