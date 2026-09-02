using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class OrderTimingRequestValidator : AbstractValidator<OrderTimingRequest>
{
    public OrderTimingRequestValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Station).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ItemType).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrepSeconds).GreaterThan(0);
    }
}
