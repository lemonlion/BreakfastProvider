using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Models.Requests;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Validators;

public class DailySpecialOrderRequestValidator : AbstractValidator<DailySpecialOrderRequest>
{
    public DailySpecialOrderRequestValidator(IOptions<DailySpecialsConfig> config)
    {
        RuleFor(x => x.SpecialId)
            .NotEmpty()
            .WithMessage("'Special Id' is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.Quantity)
            .LessThanOrEqualTo(config.Value.MaxOrdersPerSpecial)
            .WithMessage($"Quantity must not exceed {config.Value.MaxOrdersPerSpecial}.");
    }
}
