using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class SpannerConfigValidator : AbstractValidator<Configuration.SpannerConfig>
{
    public SpannerConfigValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.InstanceId).NotEmpty();
        RuleFor(x => x.DatabaseId).NotEmpty();
    }
}
