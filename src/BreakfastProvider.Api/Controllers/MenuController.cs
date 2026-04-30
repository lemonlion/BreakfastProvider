using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class MenuController(
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    PubSubEventPublisher<MenuAvailabilityChangedEvent> menuAvailabilityPublisher,
    ILogger<MenuController> logger) : ControllerBase
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

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MenuItemResponse>>> GetMenu(CancellationToken cancellationToken)
    {
        const string cacheKey = "menu_items";

        if (memoryCache.TryGetValue(cacheKey, out List<MenuItemResponse>? cachedMenu))
        {
            DiagnosticsConfig.CacheHits.Add(1, new KeyValuePair<string, object?>("cache.name", "menu"));
            return cachedMenu!;
        }

        DiagnosticsConfig.CacheMisses.Add(1, new KeyValuePair<string, object?>("cache.name", "menu"));

        // Check ingredient availability via a single supplier call
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

        var result = MenuItems.Select(item => new MenuItemResponse
        {
            Name = item.Name,
            Description = item.Description,
            RequiredIngredients = item.RequiredIngredients,
            IsAvailable = ingredientsAvailable
        }).OrderBy(m => m.Name).ToList();

        if (ingredientsAvailable)
            memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        await menuAvailabilityPublisher.PublishEvent(new MenuAvailabilityChangedEvent
        {
            ItemName = "All Items",
            IsAvailable = ingredientsAvailable,
            Reason = ingredientsAvailable ? "Supplier confirmed availability" : "Supplier unavailable",
            ChangedAt = DateTime.UtcNow
        }, cancellationToken);

        return result;
    }

    [HttpDelete("cache")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ClearCache()
    {
        memoryCache.Remove("menu_items");
        return NoContent();
    }
}
