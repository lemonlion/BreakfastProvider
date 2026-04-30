using FluentValidation;
using BreakfastProvider.Api.Models.Requests;

namespace BreakfastProvider.Api.Validators;

public class CustomerPreferenceRequestValidator : AbstractValidator<CustomerPreferenceRequest>
{
    public CustomerPreferenceRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PreferredMilkType).MaximumLength(50);
        RuleFor(x => x.FavouriteItem).MaximumLength(100);
    }
}
