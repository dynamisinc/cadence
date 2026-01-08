using FluentValidation;
using DynamisReferenceApp.Api.Tools.Notes.Models.DTOs;

namespace DynamisReferenceApp.Api.Tools.Notes.Validators;

public class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequest>
{
    public CreateNoteRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters");

        RuleFor(x => x.Content)
            .MaximumLength(10000).WithMessage("Content must not exceed 10000 characters");
    }
}

public class UpdateNoteRequestValidator : AbstractValidator<UpdateNoteRequest>
{
    public UpdateNoteRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters");

        RuleFor(x => x.Content)
            .MaximumLength(10000).WithMessage("Content must not exceed 10000 characters");
    }
}
