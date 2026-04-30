using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class ToppingRequestValidator : AbstractValidator<ToppingRequest>
{
    public ToppingRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("'Name' is required.");

        RuleFor(x => x.Name)
            .MaximumLength(100)
            .WithMessage("'Name' must not exceed 100 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Name)
            .MustNotContainHtmlOrScript("Name")
            .When(x => x.Name is not null);

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("'Category' is required.");

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .WithMessage("'Category' must not exceed 100 characters.")
            .When(x => x.Category is not null);

        RuleFor(x => x.Category)
            .MustNotContainHtmlOrScript("Category")
            .When(x => x.Category is not null);
    }
}
