using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Services;

public class ToppingService(
    IOptions<FeatureSwitchesConfig> featureSwitches,
    PubSubEventPublisher<ToppingCreatedEvent> toppingCreatedPublisher,
    ILogger<ToppingService> logger) : IToppingService
{
    private static readonly List<ToppingResponse> Toppings =
    [
        new() { ToppingId = Guid.Parse("11111111-0000-0000-0000-000000000001"), Name = "Raspberries", Category = "Fruit" },
        new() { ToppingId = Guid.Parse("11111111-0000-0000-0000-000000000002"), Name = "Blueberries", Category = "Fruit" },
        new() { ToppingId = Guid.Parse("11111111-0000-0000-0000-000000000003"), Name = "Maple Syrup", Category = "Syrup" },
        new() { ToppingId = Guid.Parse("11111111-0000-0000-0000-000000000004"), Name = "Whipped Cream", Category = "Cream" },
        new() { ToppingId = Guid.Parse("11111111-0000-0000-0000-000000000005"), Name = "Chocolate Chips", Category = "Chocolate" }
    ];

    public List<ToppingResponse> GetAvailableToppings()
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.GetAvailable");

        var result = LoadToppings();
        result = ApplyFeatureFlags(result);

        activity?.SetTag("toppings.count", result.Count);
        return result;
    }

    public async Task<ToppingResponse> CreateToppingAsync(ToppingRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.Create");

        var topping = BuildTopping(request);
        await PublishToppingCreatedAsync(topping, cancellationToken);

        return topping;
    }

    public ToppingResponse? UpdateTopping(Guid toppingId, UpdateToppingRequest request)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.Update");
        activity?.SetTag("topping.id", toppingId.ToString());

        var existing = FindTopping(toppingId);
        if (existing is null)
        {
            activity?.SetTag("topping.found", false);
            return null;
        }

        activity?.SetTag("topping.found", true);
        var updated = ApplyUpdate(toppingId, request);

        logger.LogInformation("Updated topping {ToppingId}: {Name} ({Category})", toppingId, updated.Name, updated.Category);
        return updated;
    }

    public bool DeleteTopping(Guid toppingId)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.Delete");
        activity?.SetTag("topping.id", toppingId.ToString());

        var existing = FindTopping(toppingId);
        if (existing is null)
        {
            activity?.SetTag("topping.found", false);
            return false;
        }

        activity?.SetTag("topping.found", true);
        ConfirmDeletion(toppingId);

        return true;
    }

    private List<ToppingResponse> LoadToppings()
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.LoadToppings");

        var result = Toppings.ToList();
        activity?.SetTag("toppings.total", result.Count);
        return result;
    }

    private List<ToppingResponse> ApplyFeatureFlags(List<ToppingResponse> toppings)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.ApplyFeatureFlags");
        activity?.SetTag("feature.raspberry_enabled", featureSwitches.Value.IsRaspberryToppingEnabled);

        if (!featureSwitches.Value.IsRaspberryToppingEnabled)
        {
            toppings = toppings.Where(t => t.Name != "Raspberries").ToList();
            logger.LogInformation("Raspberry topping filtered out by feature flag");
        }

        activity?.SetTag("toppings.after_filter", toppings.Count);
        return toppings;
    }

    private ToppingResponse BuildTopping(ToppingRequest request)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.BuildTopping");

        var topping = new ToppingResponse
        {
            ToppingId = Guid.NewGuid(),
            Name = request.Name!,
            Category = request.Category!
        };

        activity?.SetTag("topping.id", topping.ToppingId.ToString());
        activity?.SetTag("topping.name", topping.Name);
        logger.LogInformation("Built new topping {ToppingId}: {Name} ({Category})", topping.ToppingId, topping.Name, topping.Category);

        return topping;
    }

    private async Task PublishToppingCreatedAsync(ToppingResponse topping, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.PublishCreated");
        activity?.SetTag("topping.id", topping.ToppingId.ToString());

        await toppingCreatedPublisher.PublishEvent(new ToppingCreatedEvent
        {
            ToppingId = topping.ToppingId,
            Name = topping.Name,
            Category = topping.Category,
            IsSeasonal = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private ToppingResponse? FindTopping(Guid toppingId)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.FindTopping");
        activity?.SetTag("topping.id", toppingId.ToString());

        var topping = Toppings.FirstOrDefault(t => t.ToppingId == toppingId);
        activity?.SetTag("topping.found", topping is not null);
        return topping;
    }

    private ToppingResponse ApplyUpdate(Guid toppingId, UpdateToppingRequest request)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.ApplyUpdate");
        activity?.SetTag("topping.id", toppingId.ToString());

        return new ToppingResponse
        {
            ToppingId = toppingId,
            Name = request.Name!,
            Category = request.Category!
        };
    }

    private void ConfirmDeletion(Guid toppingId)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ToppingService.ConfirmDeletion");
        activity?.SetTag("topping.id", toppingId.ToString());

        logger.LogInformation("Topping {ToppingId} deleted", toppingId);
    }
}
