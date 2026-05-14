using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class UpdateChefNoteRequestValidator : AbstractValidator<UpdateChefNoteRequest>
{
    public UpdateChefNoteRequestValidator()
    {
        RuleFor(x => x.NoteText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Category).MaximumLength(100);
    }
}
