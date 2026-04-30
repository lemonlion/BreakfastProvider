using FluentValidation;
using BreakfastProvider.Api.Configuration;

namespace BreakfastProvider.Api.Validators;

public class CosmosConfigValidator : AbstractValidator<CosmosConfig>
{
    public CosmosConfigValidator()
    {
        RuleFor(x => x.ConnectionString)
            .NotEmpty().WithMessage("CosmosConfig:ConnectionString must not be empty.");

        RuleFor(x => x.DatabaseName)
            .NotEmpty().WithMessage("CosmosConfig:DatabaseName must not be empty.");
    }
}
