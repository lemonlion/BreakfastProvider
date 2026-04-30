namespace BreakfastProvider.Api.Models.Requests;

public record CustomerPreferenceRequest
{
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? PreferredMilkType { get; init; }
    public bool LikesExtraToppings { get; init; }
    public string? FavouriteItem { get; init; }
}
