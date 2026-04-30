using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    private static readonly string[] AllowedStatuses = ["Preparing", "Ready", "Completed", "Cancelled"];

    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("'Status' is required.");

        RuleFor(x => x.Status)
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage($"'Status' must be one of: {string.Join(", ", AllowedStatuses)}.")
            .When(x => !string.IsNullOrEmpty(x.Status));
    }
}
