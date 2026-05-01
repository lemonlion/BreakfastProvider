using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using Microsoft.Extensions.Caching.Memory;

namespace BreakfastProvider.Api.Services;

public class MenuService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    PubSubEventPublisher<MenuAvailabilityChangedEvent> menuAvailabilityPublisher,
    ILogger<MenuService> logger) : IMenuService
{
    private static readonly List<MenuItemResponse> MenuItems =
    [
        new()
        {
            Name = "Classic Pancakes",
            Description = "Fluffy pancakes made with fresh milk, eggs, and flour",
            RequiredIngredients = ["Milk", "Eggs", "Flour"],
            IsAvailable = true
        },
        new()
        {
            Name = "Belgian Waffles",
            Description = "Crispy waffles with butter, milk, eggs, and flour",
            RequiredIngredients = ["Milk", "Eggs", "Flour", "Butter"],
            IsAvailable = true
        },
        new()
        {
            Name = "Goat Milk Pancakes",
            Description = "Specialty pancakes made with fresh goat milk",
            RequiredIngredients = ["Goat Milk", "Eggs", "Flour"],
            IsAvailable = true
        }
    ];

    public async Task<(List<MenuItemResponse> Items, bool FromCache)> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MenuService.GetMenu");

        var cached = CheckCache();
        if (cached is not null)
            return (cached, true);

        var ingredientsAvailable = await CheckIngredientAvailabilityAsync(cancellationToken);
        var menu = BuildMenu(ingredientsAvailable);
        await PublishAvailabilityChangeAsync(ingredientsAvailable, cancellationToken);

        return (menu, false);
    }

    public void ClearCache()
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MenuService.ClearCache");
        memoryCache.Remove("menu_items");
        logger.LogInformation("Menu cache cleared");
    }

    private List<MenuItemResponse>? CheckCache()
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MenuService.CheckCache");

        if (memoryCache.TryGetValue("menu_items", out List<MenuItemResponse>? cachedMenu))
        {
            DiagnosticsConfig.CacheHits.Add(1, new KeyValuePair<string, object?>("cache.name", "menu"));
            activity?.SetTag("cache.hit", true);
            logger.LogInformation("Menu cache hit");
            return cachedMenu;
        }

        DiagnosticsConfig.CacheMisses.Add(1, new KeyValuePair<string, object?>("cache.name", "menu"));
        activity?.SetTag("cache.hit", false);
        return null;
    }

    private async Task<bool> CheckIngredientAvailabilityAsync(CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MenuService.CheckIngredientAvailability");

        var supplierClient = httpClientFactory.CreateClient(HttpClientNames.SupplierService);
        bool ingredientsAvailable;

        try
        {
            using var response = await supplierClient.GetAsync("ingredients/milk/availability", cancellationToken);
            ingredientsAvailable = response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Supplier service is unavailable; marking all menu items as unavailable");
            ingredientsAvailable = false;
        }

        activity?.SetTag("ingredients.available", ingredientsAvailable);
        return ingredientsAvailable;
    }

    private List<MenuItemResponse> BuildMenu(bool ingredientsAvailable)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MenuService.BuildMenu");
        activity?.SetTag("menu.ingredients_available", ingredientsAvailable);

        var result = MenuItems.Select(item => new MenuItemResponse
        {
            Name = item.Name,
            Description = item.Description,
            RequiredIngredients = item.RequiredIngredients,
            IsAvailable = ingredientsAvailable
        }).OrderBy(m => m.Name).ToList();

        if (ingredientsAvailable)
            memoryCache.Set("menu_items", result, TimeSpan.FromMinutes(5));

        activity?.SetTag("menu.item_count", result.Count);
        return result;
    }

    private async Task PublishAvailabilityChangeAsync(bool ingredientsAvailable, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MenuService.PublishAvailabilityChange");
        activity?.SetTag("menu.available", ingredientsAvailable);

        await menuAvailabilityPublisher.PublishEvent(new MenuAvailabilityChangedEvent
        {
            ItemName = "All Items",
            IsAvailable = ingredientsAvailable,
            Reason = ingredientsAvailable ? "Supplier confirmed availability" : "Supplier unavailable",
            ChangedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
