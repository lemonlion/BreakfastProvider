using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Fakes.EventGrid;
using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Models.Events;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using Microsoft.Extensions.DependencyInjection;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Orders__Breakfast_Order_Feature : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;
    private OutboxSteps? _outboxSteps;
    private OutboxSteps OutboxSteps => _outboxSteps ??= Get<OutboxSteps>();

    public Orders__Breakfast_Order_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    #region Given

    private async Task<CompositeStep> A_pancake_batch_has_been_created()
    {
        return Sub.Steps(
            _ => Milk_is_retrieved_from_the_milk_endpoint(),
            _ => The_milk_response_should_be_successful(),
            _ => Eggs_are_retrieved_from_the_eggs_endpoint(),
            _ => The_eggs_response_should_be_successful(),
            _ => Flour_is_retrieved_from_the_flour_endpoint(),
            _ => The_flour_response_should_be_successful(),
            _ => A_pancake_request_is_submitted_with_all_ingredients(),
            _ => The_pancake_batch_response_should_be_successful(),
            _ => The_batch_id_is_captured_from_the_pancakes_response());
    }

    private async Task Milk_is_retrieved_from_the_milk_endpoint()
        => await _milkSteps.Retrieve();

    private async Task The_milk_response_should_be_successful()
        => Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Eggs_are_retrieved_from_the_eggs_endpoint()
        => await _eggsSteps.Retrieve();

    private async Task The_eggs_response_should_be_successful()
        => Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Flour_is_retrieved_from_the_flour_endpoint()
        => await _flourSteps.Retrieve();

    private async Task The_flour_response_should_be_successful()
        => Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task A_pancake_request_is_submitted_with_all_ingredients()
    {
        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
    }

    private async Task The_pancake_batch_response_should_be_successful()
    {
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
        Track.That(() => _pancakeSteps.Response.Should().NotBeNull());
        Track.That(() => _pancakeSteps.Response!.BatchId.Should().NotBeEmpty());
    }

    private async Task The_batch_id_is_captured_from_the_pancakes_response()
    {
        _orderSteps.Request.Items.Add(new TestOrderItemRequest
        {
            ItemType = OrderDefaults.PancakeItemType,
            BatchId = _pancakeSteps.Response!.BatchId,
            Quantity = 1
        });
    }

    private async Task A_valid_order_request_for_the_created_batch()
    {
        _orderSteps.Request.CustomerName = _customerName;
        _orderSteps.Request.TableNumber = 7;
    }

    #endregion

    #region When

    private async Task The_breakfast_order_is_placed()
        => await _orderSteps.Send();

    #endregion

    #region Then

    private async Task<CompositeStep> The_order_response_should_contain_a_complete_order()
    {
        return Sub.Steps(
            _ => The_order_response_http_status_should_be_created(),
            _ => The_order_response_should_be_valid_json(),
            _ => The_order_should_contain_the_customer_name(),
            _ => The_order_should_contain_the_ordered_items());
    }

    private async Task The_order_response_http_status_should_be_created()
        => Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));

    private async Task The_order_response_should_be_valid_json()
        => await _orderSteps.ParseResponse();

    private async Task The_order_should_contain_the_customer_name()
        => Track.That(() => _orderSteps.Response!.CustomerName.Should().Be(_customerName));

    private async Task The_order_should_contain_the_ordered_items()
        => Track.That(() => _orderSteps.Response!.Items.Should().HaveCount(1));

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), EventStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task An_order_created_event_should_have_been_published()
    {
        var eventStore = AppFactory.Services.GetService<IPublishedEventStore>();
        if (eventStore == null) return;

        const int maxRetries = 100;
        var retryDelay = TimeSpan.FromMilliseconds(300);

        IReadOnlyList<TestOrderCreatedEvent> orderCreatedEvents = [];
        for (var i = 0; i < maxRetries; i++)
        {
            orderCreatedEvents = await eventStore.GetPublishedEventsAsync<TestOrderCreatedEvent>();
            if (orderCreatedEvents.Any(e => e.CustomerName == _customerName))
                return;
            await Task.Delay(retryDelay);
        }

        Track.That(() => orderCreatedEvents.Should().Contain(e => e.CustomerName == _customerName));
    }

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_kitchen_service_should_have_received_a_preparation_request()
        => _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), KafkaIsUnavailableInPostDeploymentEnvironments)]
    private async Task A_recipe_log_should_have_been_published_to_kafka()
    {
        var kafkaStore = AppFactory.Services.GetService<IKafkaMessageStore>();
        if (kafkaStore == null) return;

        const int maxRetries = 50;
        var retryDelay = TimeSpan.FromMilliseconds(200);

        IReadOnlyList<(string Key, TestRecipeLogEvent Message)> recipeLogMessages = [];
        for (var i = 0; i < maxRetries; i++)
        {
            recipeLogMessages = kafkaStore.GetMessages<TestRecipeLogEvent>();
            if (recipeLogMessages.Any(m => m.Message.RecipeType == OrderDefaults.PancakeItemType))
                return;
            await Task.Delay(retryDelay);
        }

        Track.That(() => recipeLogMessages.Should().Contain(m => m.Message.RecipeType == OrderDefaults.PancakeItemType,
            "a RecipeLogEvent should have been published for the pancake recipe"));
    }

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), OutboxStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task An_outbox_message_should_have_been_written_for_the_order_created_event()
    {
        const int maxRetries = 50;
        var retryDelay = TimeSpan.FromMilliseconds(200);

        for (var i = 0; i < maxRetries; i++)
        {
            await OutboxSteps.LoadOutboxMessages();
            if (OutboxSteps.OutboxMessages!.Any(m => m.EventType == EventTypes.OrderCreated))
                break;
            await Task.Delay(retryDelay);
        }

        OutboxSteps.AssertOutboxContainsMessageForEventType(EventTypes.OrderCreated);
    }

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), OutboxStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_outbox_message_should_have_been_processed()
    {
        const int maxRetries = 50;
        var retryDelay = TimeSpan.FromMilliseconds(200);

        for (var i = 0; i < maxRetries; i++)
        {
            await OutboxSteps.LoadOutboxMessages();
            if (OutboxSteps.OutboxMessages!.Any(m => m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Processed))
                return;
            await Task.Delay(retryDelay);
        }

        OutboxSteps.AssertOutboxMessageWasProcessed(EventTypes.OrderCreated);
    }

    #endregion
}
