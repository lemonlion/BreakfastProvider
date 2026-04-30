namespace BreakfastProvider.Api.Models.Responses;

public class CustomerPreferenceResponse
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PreferredMilkType { get; set; } = string.Empty;
    public bool LikesExtraToppings { get; set; }
    public string FavouriteItem { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
