using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Models.Entities;
using FluentValidation;

namespace Cadence.Core.Features.Exercises.Validators;

/// <summary>
/// FluentValidation validator for <see cref="CreateExerciseRequest"/>.
/// Enforces required fields, string length limits, and valid enum values.
/// </summary>
public class CreateExerciseRequestValidator : AbstractValidator<CreateExerciseRequest>
{
    /// <summary>Maximum length for the exercise name.</summary>
    public const int NameMaxLength = 200;

    /// <summary>Maximum length for the exercise description.</summary>
    public const int DescriptionMaxLength = 4000;

    /// <summary>Maximum length for the exercise location.</summary>
    public const int LocationMaxLength = 200;

    /// <summary>Maximum length for the IANA time zone identifier.</summary>
    public const int TimeZoneIdMaxLength = 100;

    /// <summary>Minimum valid clock multiplier (0.5x = half speed).</summary>
    public const decimal ClockMultiplierMin = 0.5m;

    /// <summary>Maximum valid clock multiplier (20x = twenty times speed).</summary>
    public const decimal ClockMultiplierMax = 20.0m;

    /// <summary>
    /// Initializes a new instance of <see cref="CreateExerciseRequestValidator"/> with all field rules.
    /// </summary>
    public CreateExerciseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Exercise name is required.")
            .MaximumLength(NameMaxLength).WithMessage($"Exercise name must be {NameMaxLength} characters or less.");

        RuleFor(x => x.ExerciseType)
            .IsInEnum().WithMessage("Invalid exercise type. Valid values are: TTX, FE, FSE, CAX, Hybrid.");

        RuleFor(x => x.ScheduledDate)
            .NotEqual(DateOnly.MinValue).WithMessage("Scheduled date is required.");

        RuleFor(x => x.Description)
            .MaximumLength(DescriptionMaxLength).WithMessage($"Description must be {DescriptionMaxLength} characters or less.")
            .When(x => x.Description != null);

        RuleFor(x => x.Location)
            .MaximumLength(LocationMaxLength).WithMessage($"Location must be {LocationMaxLength} characters or less.")
            .When(x => x.Location != null);

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Time zone ID is required.")
            .MaximumLength(TimeZoneIdMaxLength).WithMessage($"Time zone ID must be {TimeZoneIdMaxLength} characters or less.");

        RuleFor(x => x.ClockMultiplier)
            .InclusiveBetween(ClockMultiplierMin, ClockMultiplierMax)
            .WithMessage($"Clock multiplier must be between {ClockMultiplierMin} and {ClockMultiplierMax}.");

        RuleFor(x => x.DeliveryMode)
            .IsInEnum().WithMessage("Invalid delivery mode.");

        RuleFor(x => x.TimelineMode)
            .IsInEnum().WithMessage("Invalid timeline mode.");
    }
}
