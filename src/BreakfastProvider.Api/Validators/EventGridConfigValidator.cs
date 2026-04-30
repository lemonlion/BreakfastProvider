using FluentValidation;
using BreakfastProvider.Api.Configuration;

namespace BreakfastProvider.Api.Validators;

public class EventGridConfigValidator : AbstractValidator<EventGridConfig>
{
    public EventGridConfigValidator()
    {
        When(x => x.IsEnabled, () =>
        {
            RuleFor(x => x.Endpoint)
                .NotEmpty().WithMessage("EventGridConfig:Endpoint must not be empty when EventGrid is enabled.");

            RuleFor(x => x.TopicKey)
                .NotEmpty().WithMessage("EventGridConfig:TopicKey must not be empty when EventGrid is enabled.");
        });
    }
}
