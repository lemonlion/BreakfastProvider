using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class StaffMemberRequestValidator : AbstractValidator<StaffMemberRequest>
{
    private static readonly string[] ValidRoles = ["Chef", "Sous Chef", "Line Cook", "Prep Cook", "Server", "Host", "Manager", "Dishwasher"];

    public StaffMemberRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("'Name' is required.");

        RuleFor(x => x.Name)
            .MustNotContainHtmlOrScript("Name")
            .When(x => x.Name is not null);

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("'Role' is required.");

        RuleFor(x => x.Role)
            .Must(role => ValidRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"'Role' must be one of: {string.Join(", ", ValidRoles)}.")
            .When(x => !string.IsNullOrEmpty(x.Role));

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("'Email' is required.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("'Email' must be a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Email)
            .MustNotContainHtmlOrScript("Email")
            .When(x => x.Email is not null);
    }
}
