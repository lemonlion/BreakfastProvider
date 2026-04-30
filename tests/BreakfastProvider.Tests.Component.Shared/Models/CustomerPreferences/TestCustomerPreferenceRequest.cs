namespace BreakfastProvider.Tests.Component.Shared.Models.CustomerPreferences;

public class TestCustomerPreferenceRequest
{
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? PreferredMilkType { get; set; }
    public bool LikesExtraToppings { get; set; }
    public string? FavouriteItem { get; set; }
}
