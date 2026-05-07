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

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Infrastructure;

public class Infrastructure_Telemetry_Tests : BaseFixture
{
    private readonly InMemoryLoggerProvider _logProvider = new();
    private GetMilkSteps _milkSteps = null!;
    private GetEggsSteps _eggsSteps = null!;
    private GetFlourSteps _flourSteps = null!;
    private PostPancakesSteps _pancakeSteps = null!;
    private PostOrderSteps _orderSteps = null!;
    private string _customerName = null!;

    public Infrastructure_Telemetry_Tests() : base(delayAppCreation: true) { }

    [Fact]
    [HappyPath]
    public void Creating_an_order_should_emit_a_structured_log_entry()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.The_application_is_configured_with_in_memory_log_capture())
            .And(x => x.A_pancake_batch_has_been_created())
            .When(x => x.The_order_is_submitted())
            .Then(x => x.A_structured_log_entry_should_have_been_captured_for_order_creation())
            .BDDfy();
    }

    #region Steps

    private void The_application_is_configured_with_in_memory_log_capture()
    {
        CreateAppAndClient(additionalServices: services =>
        {
            services.AddSingleton<ILoggerFactory>(new LoggerFactory([_logProvider]));
        });

        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
    }

    private async Task A_pancake_batch_has_been_created()
    {
        await _milkSteps.Retrieve();
        await _eggsSteps.Retrieve();
        await _flourSteps.Retrieve();

        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
    }

    private async Task The_order_is_submitted()
    {
        _customerName = $"TelemetryTest_{Random.Shared.NextInt64()}";
        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = _customerName,
            TableNumber = 5,
            Items =
            [
                new TestOrderItemRequest
                {
                    ItemType = OrderDefaults.PancakeItemType,
                    BatchId = _pancakeSteps.Response!.BatchId,
                    Quantity = 1
                }
            ]
        };
        await _orderSteps.Send();
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private void A_structured_log_entry_should_have_been_captured_for_order_creation()
    {
        _logProvider.Entries.Should().Contain(e => e.Message.Contains("created for customer"));
        _logProvider.Entries.Should().Contain(e => e.Message.Contains(_customerName));
        _logProvider.Entries.Should().Contain(e => e.Message.Contains("1 items"));
    }

    #endregion
}
