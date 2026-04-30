using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class InventoryItemRequestValidator : AbstractValidator<InventoryItemRequest>
{
    public InventoryItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("'Name' is required.");

        RuleFor(x => x.Name)
            .MustNotContainHtmlOrScript("Name")
            .When(x => x.Name is not null);

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("'Category' is required.");

        RuleFor(x => x.Category)
            .MustNotContainHtmlOrScript("Category")
            .When(x => x.Category is not null);

        RuleFor(x => x.Unit)
            .NotEmpty()
            .WithMessage("'Unit' is required.");

        RuleFor(x => x.Unit)
            .MustNotContainHtmlOrScript("Unit")
            .When(x => x.Unit is not null);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("'Quantity' must be greater than or equal to zero.");

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0)
            .WithMessage("'Reorder Level' must be greater than or equal to zero.");
    }
}
