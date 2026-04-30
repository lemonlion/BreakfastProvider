using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class ReservationRequestValidator : AbstractValidator<ReservationRequest>
{
    public ReservationRequestValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("'Customer Name' is required.");

        RuleFor(x => x.CustomerName)
            .MustNotContainHtmlOrScript("Customer Name")
            .When(x => x.CustomerName is not null);

        RuleFor(x => x.TableNumber)
            .InclusiveBetween(1, 50)
            .WithMessage("'Table Number' must be between 1 and 50.");

        RuleFor(x => x.PartySize)
            .InclusiveBetween(1, 20)
            .WithMessage("'Party Size' must be between 1 and 20.");

        RuleFor(x => x.ReservedAt)
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("'Reserved At' must be in the future.");

        RuleFor(x => x.ContactPhone)
            .MustNotContainHtmlOrScript("Contact Phone")
            .When(x => x.ContactPhone is not null);
    }
}
