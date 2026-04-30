using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Logging;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

#pragma warning disable CS1998
public partial class Infrastructure__Telemetry_Feature : BaseFixture
{
    private GetMilkSteps _milkSteps = null!;
    private GetEggsSteps _eggsSteps = null!;
    private GetFlourSteps _flourSteps = null!;
    private PostPancakesSteps _pancakeSteps = null!;
    private PostOrderSteps _orderSteps = null!;

    private readonly InMemoryLoggerProvider _logProvider = new();

    public Infrastructure__Telemetry_Feature() : base(delayAppCreation: true)
    {
    }

    #region Given

    private async Task The_application_is_configured_with_an_in_memory_log_capture()
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

    private async Task<CompositeStep> A_pancake_batch_has_been_created()
    {
        return Sub.Steps(
            _ => A_pancake_request_is_submitted_with_ingredients(),
            _ => The_pancake_batch_should_be_successful());
    }

    private async Task A_pancake_request_is_submitted_with_ingredients()
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
    }

    private async Task The_pancake_batch_should_be_successful()
    {
        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
    }

    private async Task A_valid_order_request()
    {
        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"TelemetryTest_{Random.Shared.NextInt64()}",
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
    }

    #endregion

    #region When

    private async Task The_order_is_submitted()
    {
        await _orderSteps.Send();
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region Then

    private async Task<CompositeStep> A_structured_log_entry_should_have_been_captured_for_order_creation()
    {
        return Sub.Steps(
            _ => The_log_should_contain_an_order_created_message(),
            _ => The_log_message_should_include_the_customer_name(),
            _ => The_log_message_should_include_the_item_count());
    }

    private async Task The_log_should_contain_an_order_created_message()
        => _logProvider.Entries.Should().Contain(e => e.Message.Contains("created for customer"));

    private async Task The_log_message_should_include_the_customer_name()
        => _logProvider.Entries.Should().Contain(e =>
            e.Message.Contains(_orderSteps.Request.CustomerName!));

    private async Task The_log_message_should_include_the_item_count()
        => _logProvider.Entries.Should().Contain(e =>
            e.Message.Contains("1 items"));

    #endregion
}
