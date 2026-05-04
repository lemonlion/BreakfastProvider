using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Models.Requests;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Validators;

public class MuffinRequestValidator : AbstractValidator<MuffinRequest>
{
    public MuffinRequestValidator(IOptions<ToppingRulesConfig> toppingRules)
    {
        RuleFor(x => x.Milk).NotEmpty().WithMessage("'Milk' is required.");
        RuleFor(x => x.Milk).MustNotContainHtmlOrScript("Milk").When(x => x.Milk is not null);
        RuleFor(x => x.Flour).NotEmpty().WithMessage("'Flour' is required.");
        RuleFor(x => x.Flour).MustNotContainHtmlOrScript("Flour").When(x => x.Flour is not null);
        RuleFor(x => x.Eggs).NotEmpty().WithMessage("'Eggs' is required.");
        RuleFor(x => x.Eggs).MustNotContainHtmlOrScript("Eggs").When(x => x.Eggs is not null);
        RuleFor(x => x.Apples).NotEmpty().WithMessage("'Apples' is required.");
        RuleFor(x => x.Apples).MustNotContainHtmlOrScript("Apples").When(x => x.Apples is not null);
        RuleFor(x => x.Cinnamon).NotEmpty().WithMessage("'Cinnamon' is required.");
        RuleFor(x => x.Cinnamon).MustNotContainHtmlOrScript("Cinnamon").When(x => x.Cinnamon is not null);

        RuleFor(x => x.Baking).NotNull().WithMessage("'Baking' profile is required.");
        When(x => x.Baking is not null, () =>
        {
            RuleFor(x => x.Baking!.Temperature)
                .InclusiveBetween(150, 220)
                .WithMessage("Baking temperature must be between 150 and 220 degrees.");
            RuleFor(x => x.Baking!.DurationMinutes)
                .InclusiveBetween(10, 60)
                .WithMessage("Baking duration must be between 10 and 60 minutes.");
            RuleFor(x => x.Baking!.PanType).NotEmpty().WithMessage("'Pan Type' is required.");
            RuleFor(x => x.Baking!.PanType).MustNotContainHtmlOrScript("Pan Type").When(x => x.Baking!.PanType is not null);
        });

        RuleForEach(x => x.Toppings).ChildRules(topping =>
        {
            topping.RuleFor(t => t.Name).NotEmpty().WithMessage("Topping 'Name' is required.");
            topping.RuleFor(t => t.Name).MustNotContainHtmlOrScript("Topping Name").When(t => t.Name is not null);
            topping.RuleFor(t => t.Amount).NotEmpty().WithMessage("Topping 'Amount' is required.");
            topping.RuleFor(t => t.Amount).MustNotContainHtmlOrScript("Topping Amount").When(t => t.Amount is not null);
        });

        RuleFor(x => x.Toppings)
            .Must(t => t.Count <= toppingRules.Value.MaxToppingsPerItem)
            .WithMessage($"Maximum toppings exceeded. Limit is {toppingRules.Value.MaxToppingsPerItem}.");
    }
}
