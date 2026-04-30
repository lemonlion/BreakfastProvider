using FluentValidation;
using BreakfastProvider.Api.Models.Requests;

namespace BreakfastProvider.Api.Validators;

public class FeedbackRequestValidator : AbstractValidator<FeedbackRequest>
{
    public FeedbackRequestValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
