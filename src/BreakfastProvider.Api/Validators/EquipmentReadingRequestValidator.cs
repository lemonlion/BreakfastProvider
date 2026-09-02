using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class EquipmentReadingRequestValidator : AbstractValidator<EquipmentReadingRequest>
{
    public EquipmentReadingRequestValidator()
    {
        RuleFor(x => x.EquipmentId).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Metric).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(200);
    }
}
