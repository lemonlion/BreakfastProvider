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
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Orders;

public class Orders_Breakfast_Order_Tests : BaseFixture
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

    public Orders_Breakfast_Order_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    private async Task CreatePancakeBatch()
    {
        await _milkSteps.Retrieve();
        await _milkSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        await _eggsSteps.Retrieve();
        await _eggsSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        await _flourSteps.Retrieve();
        await _flourSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();

        await _pancakeSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        await _pancakeSteps.Response.Should().NotBeNull();
        await _pancakeSteps.Response!.BatchId.Should().NotBeEqualTo(Guid.Empty);

        _orderSteps.Request.Items.Add(new TestOrderItemRequest
        {
            ItemType = OrderDefaults.PancakeItemType,
            BatchId = _pancakeSteps.Response!.BatchId,
            Quantity = 1
        });
    }

    private void SetupValidOrderRequest()
    {
        _orderSteps.Request.CustomerName = _customerName;
        _orderSteps.Request.TableNumber = 7;
    }

    [Test]
    [HappyPath]
    public async Task Valid_order_should_be_created_and_an_event_published()
    {
        // Given
        await CreatePancakeBatch();
        SetupValidOrderRequest();

        // When
        await _orderSteps.Send();

        // Then
        await _orderSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        await _orderSteps.Response!.CustomerName.Should().BeEqualTo(_customerName);
        await _orderSteps.Response!.Items.Should().HaveCount(1);

        // And an order created event should have been published
        if (!Settings.RunAgainstExternalServiceUnderTest)
        {
            var eventStore = AppFactory.Services.GetService<IPublishedEventStore>();
            if (eventStore != null)
            {
                const int maxRetries = 100;
                var retryDelay = TimeSpan.FromMilliseconds(300);

                IReadOnlyList<TestOrderCreatedEvent> orderCreatedEvents = [];
                for (var i = 0; i < maxRetries; i++)
                {
                    orderCreatedEvents = await eventStore.GetPublishedEventsAsync<TestOrderCreatedEvent>();
                    if (orderCreatedEvents.Any(e => e.CustomerName == _customerName))
                        break;
                    await Task.Delay(retryDelay);
                }

                await orderCreatedEvents.Should().Contain(e => e.CustomerName == _customerName);
            }

            // And the kitchen service should have received a preparation request
            _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
        }
    }

    [Test]
    public async Task Creating_an_order_should_produce_an_audit_log_entry_and_events()
    {
        // Given
        await CreatePancakeBatch();
        SetupValidOrderRequest();

        // When
        await _orderSteps.Send();

        // Then
        await _orderSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        await _orderSteps.Response!.CustomerName.Should().BeEqualTo(_customerName);
        await _orderSteps.Response!.Items.Should().HaveCount(1);

        // And an order created event should have been published
        if (!Settings.RunAgainstExternalServiceUnderTest)
        {
            var eventStore = AppFactory.Services.GetService<IPublishedEventStore>();
            if (eventStore != null)
            {
                const int maxRetries = 100;
                var retryDelay = TimeSpan.FromMilliseconds(300);

                IReadOnlyList<TestOrderCreatedEvent> orderCreatedEvents = [];
                for (var i = 0; i < maxRetries; i++)
                {
                    orderCreatedEvents = await eventStore.GetPublishedEventsAsync<TestOrderCreatedEvent>();
                    if (orderCreatedEvents.Any(e => e.CustomerName == _customerName))
                        break;
                    await Task.Delay(retryDelay);
                }

                await orderCreatedEvents.Should().Contain(e => e.CustomerName == _customerName);
            }
        }

        // And a recipe log should have been published to Kafka
        if (!Settings.RunAgainstExternalServiceUnderTest)
        {
            var kafkaStore = AppFactory.Services.GetService<IKafkaMessageStore>();
            if (kafkaStore != null)
            {
                const int maxRetries = 50;
                var retryDelay = TimeSpan.FromMilliseconds(200);

                IReadOnlyList<(string Key, TestRecipeLogEvent Message)> recipeLogMessages = [];
                for (var i = 0; i < maxRetries; i++)
                {
                    recipeLogMessages = kafkaStore.GetMessages<TestRecipeLogEvent>();
                    if (recipeLogMessages.Any(m => m.Message.RecipeType == OrderDefaults.PancakeItemType))
                        break;
                    await Task.Delay(retryDelay);
                }

                await recipeLogMessages.Should().Contain(m => m.Message.RecipeType == OrderDefaults.PancakeItemType,
                    "a RecipeLogEvent should have been published for the pancake recipe");
            }
        }
    }

    [Test]
    public async Task Creating_an_order_should_write_an_outbox_message_that_gets_processed()
    {
        // Given
        await CreatePancakeBatch();
        SetupValidOrderRequest();

        // When
        await _orderSteps.Send();

        // Then
        await _orderSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        await _orderSteps.Response!.CustomerName.Should().BeEqualTo(_customerName);
        await _orderSteps.Response!.Items.Should().HaveCount(1);

        if (!Settings.RunAgainstExternalServiceUnderTest)
        {
            // And an outbox message should have been written for the order created event
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

            // And the outbox message should have been processed
            for (var i = 0; i < maxRetries; i++)
            {
                await OutboxSteps.LoadOutboxMessages();
                if (OutboxSteps.OutboxMessages!.Any(m => m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Processed))
                    break;
                await Task.Delay(retryDelay);
            }

            OutboxSteps.AssertOutboxMessageWasProcessed(EventTypes.OrderCreated);
        }
    }
}
