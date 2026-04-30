namespace BreakfastProvider.Tests.Component.Shared.Models.Validation;

public record VerifiableErrorResult(string? ErrorMessage, string ResponseStatus = "Bad Request");
