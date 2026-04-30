using FluentValidation;
using BreakfastProvider.Api.Configuration;

namespace BreakfastProvider.Api.Validators;

public class PubSubConfigValidator : AbstractValidator<PubSubConfig>
{
    public PubSubConfigValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId must not be empty.");

        RuleFor(x => x.PublishTimeoutInMilliseconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage("PublishTimeoutInMilliseconds must be 0 or greater.");

        RuleForEach(x => x.PublisherConfigurations)
            .ChildRules(config =>
            {
                config.RuleFor(x => x.Value.TopicId)
                    .NotEmpty().WithMessage("Publisher configuration TopicId must not be empty.");
            })
            .When(x => x.PublisherConfigurations.Any());
    }
}
