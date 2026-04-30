namespace BreakfastProvider.Tests.Component.LightBDD.Models.Validation;

public record InvalidFieldFromRequest(string? Field, object? Value, string Reason);
