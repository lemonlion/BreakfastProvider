using Microsoft.EntityFrameworkCore;

namespace BreakfastProvider.Api.Reporting;

public class ReportingQuery
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<OrderSummary> GetOrderSummaries(ReportingDbContext dbContext)
        => dbContext.OrderSummaries;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<RecipeReport> GetRecipeReports(ReportingDbContext dbContext)
        => dbContext.RecipeReports;

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

    public async Task<List<RecipeTypeCount>> GetPopularRecipes(ReportingDbContext dbContext)
    {
        return await dbContext.RecipeReports
            .GroupBy(r => r.RecipeType)
            .Select(g => new RecipeTypeCount { RecipeType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<BatchCompletionRecord> GetBatchCompletions(ReportingDbContext dbContext)
        => dbContext.BatchCompletionRecords;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<IngredientShipment> GetIngredientShipments(ReportingDbContext dbContext)
        => dbContext.IngredientShipments;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<EquipmentAlert> GetEquipmentAlerts(ReportingDbContext dbContext)
        => dbContext.EquipmentAlerts;
}

public class IngredientUsage
{
    public string Ingredient { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecipeTypeCount
{
    public string RecipeType { get; set; } = string.Empty;
    public int Count { get; set; }
}
