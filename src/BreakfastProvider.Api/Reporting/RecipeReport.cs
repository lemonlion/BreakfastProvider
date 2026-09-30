namespace BreakfastProvider.Api.Reporting;

/// <summary>The recipe of one pancake, waffle or muffin batch, as logged to Kafka.</summary>
public class RecipeReport
{
    /// <summary>The reporting database's key for the row.</summary>
    public int Id { get; set; }

    /// <summary>The id of the batch the recipe was logged for; the recipe log event carries it as OrderId.</summary>
    public Guid OrderId { get; set; }

    /// <summary>The recipe type: Pancakes, Waffles or AppleCinnamonMuffins.</summary>
    public string RecipeType { get; set; } = string.Empty;

    /// <summary>The batch's ingredients, comma-separated.</summary>
    public string Ingredients { get; set; } = string.Empty;

    /// <summary>The batch's toppings, comma-separated; empty when it has none.</summary>
    public string Toppings { get; set; } = string.Empty;

    /// <summary>When the recipe was logged, in UTC.</summary>
    public DateTime LoggedAt { get; set; }
}
