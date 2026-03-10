using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Validators;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Exercises.Validators;

/// <summary>
/// Tests for <see cref="CreateExerciseRequestValidator"/>.
/// Each test constructs a request that differs from the valid baseline in exactly one field.
/// Properties use init-only setters, so each variation constructs a fresh object via the factory method.
/// </summary>
public class CreateExerciseRequestValidatorTests
{
    private readonly CreateExerciseRequestValidator _validator = new();

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
    public void ValidRequest_WithOptionalFieldsNull_PassesValidation()
    {
        // Description and Location are null by default in ValidRequest().
        var result = _validator.Validate(ValidRequest(description: null, location: null));
        result.IsValid.Should().BeTrue();
    }

    // =========================================================================
    // Name
    // =========================================================================

    [Fact]
    public void Name_Empty_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(name: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.Name));
    }

    [Fact]
    public void Name_Whitespace_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(name: "   "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.Name));
    }

    [Fact]
    public void Name_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(name: new string('x', CreateExerciseRequestValidator.NameMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.Name));
    }

    [Fact]
    public void Name_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(name: new string('x', CreateExerciseRequestValidator.NameMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.Name));
    }

    // =========================================================================
    // ExerciseType (enum validation)
    // =========================================================================

    [Fact]
    public void ExerciseType_TTX_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(exerciseType: ExerciseType.TTX));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.ExerciseType));
    }

    [Fact]
    public void ExerciseType_FE_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(exerciseType: ExerciseType.FE));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.ExerciseType));
    }

    [Fact]
    public void ExerciseType_FSE_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(exerciseType: ExerciseType.FSE));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.ExerciseType));
    }

    [Fact]
    public void ExerciseType_CAX_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(exerciseType: ExerciseType.CAX));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.ExerciseType));
    }

    [Fact]
    public void ExerciseType_Hybrid_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(exerciseType: ExerciseType.Hybrid));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.ExerciseType));
    }

    [Fact]
    public void ExerciseType_InvalidEnumValue_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(exerciseType: (ExerciseType)999));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.ExerciseType));
    }

    // =========================================================================
    // ScheduledDate
    // =========================================================================

    [Fact]
    public void ScheduledDate_ValidDate_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(scheduledDate: new DateOnly(2026, 9, 15)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.ScheduledDate));
    }

    [Fact]
    public void ScheduledDate_MinValue_FailsValidation()
    {
        // Pass MinValue explicitly via the nullable overload so the factory does not substitute the baseline date.
        var request = new CreateExerciseRequest
        {
            Name = "Hurricane Response TTX 2026",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.MinValue,
            TimeZoneId = "America/Los_Angeles",
            ClockMultiplier = 1.0m,
            DeliveryMode = DeliveryMode.ClockDriven,
            TimelineMode = TimelineMode.RealTime
        };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.ScheduledDate));
    }

    // =========================================================================
    // Description (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void Description_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(description: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.Description));
    }

    [Fact]
    public void Description_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(description: new string('x', CreateExerciseRequestValidator.DescriptionMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.Description));
    }

    [Fact]
    public void Description_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(description: new string('x', CreateExerciseRequestValidator.DescriptionMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.Description));
    }

    // =========================================================================
    // Location (optional — max length enforced only when non-null)
    // =========================================================================

    [Fact]
    public void Location_Null_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(location: null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.Location));
    }

    [Fact]
    public void Location_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(location: new string('x', CreateExerciseRequestValidator.LocationMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.Location));
    }

    [Fact]
    public void Location_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(location: new string('x', CreateExerciseRequestValidator.LocationMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.Location));
    }

    // =========================================================================
    // TimeZoneId
    // =========================================================================

    [Fact]
    public void TimeZoneId_Empty_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(timeZoneId: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.TimeZoneId));
    }

    [Fact]
    public void TimeZoneId_Whitespace_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(timeZoneId: "   "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.TimeZoneId));
    }

    [Fact]
    public void TimeZoneId_ExactlyMaxLength_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(timeZoneId: new string('x', CreateExerciseRequestValidator.TimeZoneIdMaxLength)));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.TimeZoneId));
    }

    [Fact]
    public void TimeZoneId_ExceedsMaxLength_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(timeZoneId: new string('x', CreateExerciseRequestValidator.TimeZoneIdMaxLength + 1)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.TimeZoneId));
    }

    // =========================================================================
    // ClockMultiplier (0.5 to 20.0 inclusive)
    // =========================================================================

    [Fact]
    public void ClockMultiplier_DefaultValue_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(clockMultiplier: 1.0m));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.ClockMultiplier));
    }

    [Fact]
    public void ClockMultiplier_MinValue_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(clockMultiplier: CreateExerciseRequestValidator.ClockMultiplierMin));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.ClockMultiplier));
    }

    [Fact]
    public void ClockMultiplier_MaxValue_PassesValidation()
    {
        var result = _validator.Validate(
            ValidRequest(clockMultiplier: CreateExerciseRequestValidator.ClockMultiplierMax));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.ClockMultiplier));
    }

    [Fact]
    public void ClockMultiplier_BelowMin_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(clockMultiplier: CreateExerciseRequestValidator.ClockMultiplierMin - 0.1m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.ClockMultiplier));
    }

    [Fact]
    public void ClockMultiplier_AboveMax_FailsValidation()
    {
        var result = _validator.Validate(
            ValidRequest(clockMultiplier: CreateExerciseRequestValidator.ClockMultiplierMax + 0.1m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.ClockMultiplier));
    }

    [Fact]
    public void ClockMultiplier_Zero_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(clockMultiplier: 0m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.ClockMultiplier));
    }

    // =========================================================================
    // DeliveryMode (enum validation)
    // =========================================================================

    [Fact]
    public void DeliveryMode_ClockDriven_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(deliveryMode: DeliveryMode.ClockDriven));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.DeliveryMode));
    }

    [Fact]
    public void DeliveryMode_FacilitatorPaced_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(deliveryMode: DeliveryMode.FacilitatorPaced));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.DeliveryMode));
    }

    [Fact]
    public void DeliveryMode_InvalidEnumValue_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(deliveryMode: (DeliveryMode)999));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.DeliveryMode));
    }

    // =========================================================================
    // TimelineMode (enum validation)
    // =========================================================================

    [Fact]
    public void TimelineMode_RealTime_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(timelineMode: TimelineMode.RealTime));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.TimelineMode));
    }

    [Fact]
    public void TimelineMode_Compressed_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(timelineMode: TimelineMode.Compressed));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.TimelineMode));
    }

    [Fact]
    public void TimelineMode_StoryOnly_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest(timelineMode: TimelineMode.StoryOnly));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateExerciseRequest.TimelineMode));
    }

    [Fact]
    public void TimelineMode_InvalidEnumValue_FailsValidation()
    {
        var result = _validator.Validate(ValidRequest(timelineMode: (TimelineMode)999));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExerciseRequest.TimelineMode));
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Returns a valid <see cref="CreateExerciseRequest"/> with optional per-field overrides.
    /// All parameters default to baseline valid values.
    /// </summary>
    private static CreateExerciseRequest ValidRequest(
        string name = "Hurricane Response TTX 2026",
        ExerciseType exerciseType = ExerciseType.TTX,
        DateOnly? scheduledDate = null,
        string? description = null,
        string? location = null,
        string timeZoneId = "America/Los_Angeles",
        decimal clockMultiplier = 1.0m,
        DeliveryMode deliveryMode = DeliveryMode.ClockDriven,
        TimelineMode timelineMode = TimelineMode.RealTime) => new()
    {
        Name = name,
        ExerciseType = exerciseType,
        ScheduledDate = scheduledDate ?? new DateOnly(2026, 9, 15),
        Description = description,
        Location = location,
        TimeZoneId = timeZoneId,
        ClockMultiplier = clockMultiplier,
        DeliveryMode = deliveryMode,
        TimelineMode = timelineMode
    };
}
