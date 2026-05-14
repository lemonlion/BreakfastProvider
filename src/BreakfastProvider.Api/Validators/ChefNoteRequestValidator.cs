using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class ChefNoteRequestValidator : AbstractValidator<ChefNoteRequest>
{
    public ChefNoteRequestValidator()
    {
        RuleFor(x => x.RecipeName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ChefName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NoteText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
    }
}
