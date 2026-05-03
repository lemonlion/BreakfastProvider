using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Logging;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Infrastructure;

public class Infrastructure_Telemetry_Tests : BaseFixture
{
    private readonly InMemoryLoggerProvider _logProvider = new();

    public Infrastructure_Telemetry_Tests() : base(delayAppCreation: true) { }

    [Fact]
    [HappyPath]
    public async Task Creating_an_order_should_emit_a_structured_log_entry()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given the application is configured with an in-memory log capture
        CreateAppAndClient(additionalServices: services =>
        {
            services.AddSingleton<ILoggerFactory>(new LoggerFactory([_logProvider]));
        });

        var milkSteps = Get<GetMilkSteps>();
        var eggsSteps = Get<GetEggsSteps>();
        var flourSteps = Get<GetFlourSteps>();
        var pancakeSteps = Get<PostPancakesSteps>();
        var orderSteps = Get<PostOrderSteps>();

        // And a pancake batch has been created
        await milkSteps.Retrieve();
        await eggsSteps.Retrieve();
        await flourSteps.Retrieve();

        pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = milkSteps.MilkResponse.Milk,
            Eggs = eggsSteps.EggsResponse.Eggs,
            Flour = flourSteps.FlourResponse.Flour
        };
        await pancakeSteps.Send();
        Track.That(() => pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await pancakeSteps.ParseResponse();

        // And a valid order request
        var customerName = $"TelemetryTest_{Random.Shared.NextInt64()}";
        orderSteps.Request = new TestOrderRequest
        {
            CustomerName = customerName,
            TableNumber = 5,
            Items =
            [
                new TestOrderItemRequest
                {
                    ItemType = OrderDefaults.PancakeItemType,
                    BatchId = pancakeSteps.Response!.BatchId,
                    Quantity = 1
                }
            ]
        };

        // When the order is submitted
        await orderSteps.Send();
        Track.That(() => orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));

        // Then a structured log entry should have been captured for order creation
        Track.That(() => _logProvider.Entries.Should().Contain(e => e.Message.Contains("created for customer")));
        Track.That(() => _logProvider.Entries.Should().Contain(e => e.Message.Contains(customerName)));
        Track.That(() => _logProvider.Entries.Should().Contain(e => e.Message.Contains("1 items")));
    }
}
