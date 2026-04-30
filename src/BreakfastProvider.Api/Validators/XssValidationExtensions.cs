using System.Text.RegularExpressions;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public static partial class XssValidationExtensions
{
    public static IRuleBuilderOptions<T, string?> MustNotContainHtmlOrScript<T>(
        this IRuleBuilder<T, string?> ruleBuilder, string fieldName)
    {
        return ruleBuilder
            .Must(NotContainHtmlOrScript)
            .WithMessage($"{fieldName} contains potentially dangerous content.");
    }

    private static bool NotContainHtmlOrScript(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        return !DangerousContentPattern().IsMatch(value);
    }

    [GeneratedRegex(@"<[^>]*>|javascript:|on\w+=", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousContentPattern();
}
