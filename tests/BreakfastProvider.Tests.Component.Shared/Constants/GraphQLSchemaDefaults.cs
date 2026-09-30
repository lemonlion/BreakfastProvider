namespace BreakfastProvider.Tests.Component.Shared.Constants;

/// <summary>What the published GraphQL contract declares, and how its UI is recognised.</summary>
public static class GraphQLSchemaDefaults
{
    /// <summary>The root query type. It is not the conventional <c>Query</c>.</summary>
    public const string QueryTypeName = "ReportingQuery";

    public const string OrderSummaries = "orderSummaries";
    public const string RecipeReports = "recipeReports";
    public const string IngredientUsage = "ingredientUsage";
    public const string PopularRecipes = "popularRecipes";
    public const string BatchCompletions = "batchCompletions";
    public const string IngredientShipments = "ingredientShipments";
    public const string EquipmentAlerts = "equipmentAlerts";

    /// <summary>The name the GraphQL IDE's page carries.</summary>
    public const string NitroMarker = "Nitro";

    /// <summary>
    /// The header Cloudflare adds to every response it serves. HotChocolate's default Nitro serve mode proxies the IDE
    /// from ChilliCream's CDN, whose responses carry it; the IDE the service embeds does not.
    /// </summary>
    public const string CdnRayHeader = "cf-ray";
}
