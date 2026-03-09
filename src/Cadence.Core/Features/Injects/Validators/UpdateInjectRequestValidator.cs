using Cadence.Core.Features.Injects.Models.DTOs;
using FluentValidation;

namespace Cadence.Core.Features.Injects.Validators;

/// <summary>
/// FluentValidation validator for <see cref="UpdateInjectRequest"/>.
/// Applies the same field rules as <see cref="CreateInjectRequestValidator"/>
/// since the update request has the same required fields and constraints.
/// </summary>
public class UpdateInjectRequestValidator : AbstractValidator<UpdateInjectRequest>
{
    /// <summary>Maximum length for the inject title.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>Minimum length for the inject title.</summary>
    public const int TitleMinLength = 3;

    /// <summary>Maximum length for the inject description (full content).</summary>
    public const int DescriptionMaxLength = 4000;

    /// <summary>Maximum length for the target field.</summary>
    public const int TargetMaxLength = 200;

    /// <summary>Maximum length for the source field.</summary>
    public const int SourceMaxLength = 200;

    /// <summary>Maximum length for the expected action field.</summary>
    public const int ExpectedActionMaxLength = 2000;

    /// <summary>Maximum length for the controller notes field.</summary>
    public const int ControllerNotesMaxLength = 2000;

    /// <summary>Maximum length for the trigger condition field.</summary>
    public const int TriggerConditionMaxLength = 500;

    /// <summary>Maximum length for the source reference field.</summary>
    public const int SourceReferenceMaxLength = 50;

    /// <summary>Maximum length for the responsible controller field.</summary>
    public const int ResponsibleControllerMaxLength = 200;

    /// <summary>Maximum length for the location name field.</summary>
    public const int LocationNameMaxLength = 200;

    /// <summary>Maximum length for the location type field.</summary>
    public const int LocationTypeMaxLength = 100;

    /// <summary>Maximum length for the track field.</summary>
    public const int TrackMaxLength = 100;

    /// <summary>Maximum length for the delivery method other field.</summary>
    public const int DeliveryMethodOtherMaxLength = 200;

    /// <summary>Minimum allowed priority value (Critical).</summary>
    public const int PriorityMin = 1;

    /// <summary>Maximum allowed priority value (Informational).</summary>
    public const int PriorityMax = 5;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateInjectRequestValidator"/> with all field rules.
    /// </summary>
    public UpdateInjectRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(TitleMinLength).WithMessage($"Title must be at least {TitleMinLength} characters.")
            .MaximumLength(TitleMaxLength).WithMessage($"Title must be {TitleMaxLength} characters or less.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(DescriptionMaxLength).WithMessage($"Description must be {DescriptionMaxLength} characters or less.");

        RuleFor(x => x.Target)
            .NotEmpty().WithMessage("Target is required.")
            .MaximumLength(TargetMaxLength).WithMessage($"Target must be {TargetMaxLength} characters or less.");

        RuleFor(x => x.Source)
            .MaximumLength(SourceMaxLength).WithMessage($"Source must be {SourceMaxLength} characters or less.")
            .When(x => x.Source != null);

        RuleFor(x => x.ExpectedAction)
            .MaximumLength(ExpectedActionMaxLength).WithMessage($"Expected action must be {ExpectedActionMaxLength} characters or less.")
            .When(x => x.ExpectedAction != null);

        RuleFor(x => x.ControllerNotes)
            .MaximumLength(ControllerNotesMaxLength).WithMessage($"Controller notes must be {ControllerNotesMaxLength} characters or less.")
            .When(x => x.ControllerNotes != null);

        RuleFor(x => x.TriggerCondition)
            .MaximumLength(TriggerConditionMaxLength).WithMessage($"Trigger condition must be {TriggerConditionMaxLength} characters or less.")
            .When(x => x.TriggerCondition != null);

        RuleFor(x => x.SourceReference)
            .MaximumLength(SourceReferenceMaxLength).WithMessage($"Source reference must be {SourceReferenceMaxLength} characters or less.")
            .When(x => x.SourceReference != null);

        RuleFor(x => x.Priority)
            .InclusiveBetween(PriorityMin, PriorityMax)
            .WithMessage($"Priority must be between {PriorityMin} (Critical) and {PriorityMax} (Informational).")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.InjectType)
            .IsInEnum().WithMessage("Invalid inject type.");

        RuleFor(x => x.TriggerType)
            .IsInEnum().WithMessage("Invalid trigger type.");

        RuleFor(x => x.ResponsibleController)
            .MaximumLength(ResponsibleControllerMaxLength).WithMessage($"Responsible controller must be {ResponsibleControllerMaxLength} characters or less.")
            .When(x => x.ResponsibleController != null);

        RuleFor(x => x.LocationName)
            .MaximumLength(LocationNameMaxLength).WithMessage($"Location name must be {LocationNameMaxLength} characters or less.")
            .When(x => x.LocationName != null);

        RuleFor(x => x.LocationType)
            .MaximumLength(LocationTypeMaxLength).WithMessage($"Location type must be {LocationTypeMaxLength} characters or less.")
            .When(x => x.LocationType != null);

        RuleFor(x => x.Track)
            .MaximumLength(TrackMaxLength).WithMessage($"Track must be {TrackMaxLength} characters or less.")
            .When(x => x.Track != null);

        RuleFor(x => x.DeliveryMethodOther)
            .MaximumLength(DeliveryMethodOtherMaxLength).WithMessage($"Delivery method other must be {DeliveryMethodOtherMaxLength} characters or less.")
            .When(x => x.DeliveryMethodOther != null);

        // Scenario day/time cross-field validation
        RuleFor(x => x.ScenarioDay)
            .InclusiveBetween(1, 99)
            .WithMessage("Scenario day must be between 1 and 99.")
            .When(x => x.ScenarioDay.HasValue);

        RuleFor(x => x.ScenarioDay)
            .NotNull().WithMessage("Scenario day is required when scenario time is provided.")
            .When(x => x.ScenarioTime.HasValue);
    }
}
