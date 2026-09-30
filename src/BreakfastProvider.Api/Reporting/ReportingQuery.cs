using Microsoft.EntityFrameworkCore;

namespace BreakfastProvider.Api.Reporting;

/// <summary>
/// Reporting queries over the business-intelligence database, which the service fills from its own orders and batches
/// and from the events it consumes.
/// </summary>
public class ReportingQuery
{
    /// <summary>One summary per order placed through POST /orders, recorded when the order is created.</summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<OrderSummary> GetOrderSummaries(ReportingDbContext dbContext)
        => dbContext.OrderSummaries;

    /// <summary>
    /// One report per recipe logged: every pancake, waffle and muffin batch logs its recipe to Kafka, and the reporting
    /// consumer records it.
    /// </summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<RecipeReport> GetRecipeReports(ReportingDbContext dbContext)
        => dbContext.RecipeReports;

    /// <summary>How many recipe reports list each ingredient, the most used first.</summary>
    public async Task<List<IngredientUsage>> GetIngredientUsage(ReportingDbContext dbContext)
    {
        var recipes = await dbContext.RecipeReports.ToListAsync();

        return recipes
            .SelectMany(r => r.Ingredients.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(i => i.Trim())
            .Where(i => !string.IsNullOrEmpty(i))
            .GroupBy(i => i, StringComparer.OrdinalIgnoreCase)
            .Select(g => new IngredientUsage { Ingredient = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    /// <summary>How many recipe reports each recipe type has, the most popular first.</summary>
    public async Task<List<RecipeTypeCount>> GetPopularRecipes(ReportingDbContext dbContext)
    {
        return await dbContext.RecipeReports
            .GroupBy(r => r.RecipeType)
            .Select(g => new RecipeTypeCount { RecipeType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
    }

    /// <summary>One record per completed pancake batch, from the batch-completed events on Google Cloud Pub/Sub.</summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<BatchCompletionRecord> GetBatchCompletions(ReportingDbContext dbContext)
        => dbContext.BatchCompletionRecords;

    /// <summary>
    /// One record per ingredient delivery, from the IngredientDeliveryEvent that Azure Event Grid delivers to
    /// POST /webhooks/eventgrid.
    /// </summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<IngredientShipment> GetIngredientShipments(ReportingDbContext dbContext)
        => dbContext.IngredientShipments;

    /// <summary>One record per equipment alert, from Azure Event Hub. Each pancake batch raises one for the griddle.</summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<EquipmentAlert> GetEquipmentAlerts(ReportingDbContext dbContext)
        => dbContext.EquipmentAlerts;
}

/// <summary>How many recipe reports list one ingredient.</summary>
public class IngredientUsage
{
    /// <summary>The ingredient, as the recipes name it; names that differ only in case count as one.</summary>
    public string Ingredient { get; set; } = string.Empty;

    /// <summary>How many recipe reports list it.</summary>
    public int Count { get; set; }
}

/// <summary>How many recipe reports one recipe type has.</summary>
public class RecipeTypeCount
{
    /// <summary>The recipe type: Pancakes, Waffles or AppleCinnamonMuffins.</summary>
    public string RecipeType { get; set; } = string.Empty;

    /// <summary>How many recipe reports it has.</summary>
    public int Count { get; set; }
}
