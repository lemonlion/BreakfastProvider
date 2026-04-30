using FluentValidation;
using BreakfastProvider.Api.Configuration;

namespace BreakfastProvider.Api.Validators;

public class KafkaConfigValidator : AbstractValidator<KafkaConfig>
{
    public KafkaConfigValidator()
    {
        RuleFor(x => x.BootstrapServers)
            .NotEmpty().WithMessage("BootstrapServers must not be empty.");
        
        RuleFor(x => x.MessageTimeoutInMilliseconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MessageTimeoutInMilliseconds must be 0 or greater.");

        RuleForEach(x => x.ProducerConfigurations)
            .ChildRules(config =>
            {
                config.RuleFor(x => x.Value.TopicName)
                    .NotEmpty().WithMessage("Producer configuration TopicName must not be empty.");

                config.RuleFor(x => x.Value.ApiKey)
                    .NotEmpty().WithMessage("Producer configuration ApiKey must not be empty.");

                config.RuleFor(x => x.Value.ApiSecret)
                    .NotEmpty().WithMessage("Producer configuration ApiSecret must not be empty.");
            })
            .When(x => x.ProducerConfigurations.Any());
        
        RuleForEach(x => x.ConsumerConfigurations)
            .ChildRules(config =>
            {
                config.RuleFor(x => x.Value.TopicName)
                    .NotEmpty().WithMessage("Consumer configuration TopicName must not be empty.");

                config.RuleFor(x => x.Value.ApiKey)
                    .NotEmpty().WithMessage("Consumer configuration ApiKey must not be empty.");

                config.RuleFor(x => x.Value.ApiSecret)
                    .NotEmpty().WithMessage("Consumer configuration ApiSecret must not be empty.");
            })
            .When(x => x.ConsumerConfigurations.Any());
    }
}
