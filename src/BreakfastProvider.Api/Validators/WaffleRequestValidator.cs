using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Models.Requests;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Validators;

public class WaffleRequestValidator : AbstractValidator<WaffleRequest>
{
    public WaffleRequestValidator(IOptions<ToppingRulesConfig> toppingRules)
    {
        RuleFor(x => x.Milk).NotEmpty().WithMessage("'Milk' is required.");
        RuleFor(x => x.Milk).MustNotContainHtmlOrScript("Milk").When(x => x.Milk is not null);
        RuleFor(x => x.Flour).NotEmpty().WithMessage("'Flour' is required.");
        RuleFor(x => x.Flour).MustNotContainHtmlOrScript("Flour").When(x => x.Flour is not null);
        RuleFor(x => x.Eggs).NotEmpty().WithMessage("'Eggs' is required.");
        RuleFor(x => x.Eggs).MustNotContainHtmlOrScript("Eggs").When(x => x.Eggs is not null);
        RuleFor(x => x.Butter).NotEmpty().WithMessage("'Butter' is required.");
        RuleFor(x => x.Butter).MustNotContainHtmlOrScript("Butter").When(x => x.Butter is not null);
        RuleForEach(x => x.Toppings).MustNotContainHtmlOrScript("Toppings");
        RuleFor(x => x.Toppings)
            .Must(t => t.Count <= toppingRules.Value.MaxToppingsPerItem)
            .WithMessage($"Maximum toppings exceeded. Limit is {toppingRules.Value.MaxToppingsPerItem}.");
    }
}
