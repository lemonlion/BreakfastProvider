using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class IngredientUsageRequestValidator : AbstractValidator<IngredientUsageRequest>
{
    public IngredientUsageRequestValidator()
    {
        RuleFor(x => x.IngredientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.QuantityUsed).GreaterThan(0);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RecipeName).NotEmpty().MaximumLength(200);
    }
}
