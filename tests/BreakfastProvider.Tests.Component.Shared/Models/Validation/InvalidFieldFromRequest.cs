namespace BreakfastProvider.Tests.Component.Shared.Models.Validation;

public record InvalidFieldFromRequest(string? Field, object? Value, string Reason);
