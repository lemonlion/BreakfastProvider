namespace BreakfastProvider.Tests.Component.LightBDD.Models.Validation;

public record VerifiableErrorResult(string? ErrorMessage, string ResponseStatus = "Bad Request");
