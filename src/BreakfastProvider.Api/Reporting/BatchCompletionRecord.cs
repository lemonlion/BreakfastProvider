namespace BreakfastProvider.Api.Reporting;

/// <summary>A completed pancake batch, from its batch-completed event on Google Cloud Pub/Sub.</summary>
public class BatchCompletionRecord
{
    /// <summary>The reporting database's key for the row.</summary>
    public int Id { get; set; }

    /// <summary>The batch's id, as returned by POST /pancakes.</summary>
    public Guid BatchId { get; set; }

    /// <summary>The recipe type; only pancake batches publish the event, so it is Pancakes.</summary>
    public string RecipeType { get; set; } = string.Empty;

    /// <summary>The batch's ingredients, comma-separated.</summary>
    public string Ingredients { get; set; } = string.Empty;

    /// <summary>When the batch was completed, in UTC.</summary>
    public DateTime CompletedAt { get; set; }
}
