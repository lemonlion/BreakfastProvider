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

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Orders;

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

    [Fact]
    [HappyPath]
    public void Valid_order_should_be_created_and_an_event_published()
    {
        this.Given(x => x.A_pancake_batch_is_created_and_order_request_is_ready())
            .When(x => x.The_order_is_submitted())
            .Then(x => x.The_order_should_be_created_successfully())
            .And(x => x.An_order_created_event_should_have_been_published())
            .And(x => x.The_kitchen_service_should_have_received_a_preparation_request())
            .BDDfy();
    }

    [Fact]
    public void Creating_an_order_should_produce_an_audit_log_entry_and_events()
    {
        this.Given(x => x.A_pancake_batch_is_created_and_order_request_is_ready())
            .When(x => x.The_order_is_submitted())
            .Then(x => x.The_order_should_be_created_successfully())
            .And(x => x.An_order_created_event_should_have_been_published())
            .And(x => x.A_recipe_log_should_have_been_published_to_kafka())
            .BDDfy();
    }

    [Fact]
    public void Creating_an_order_should_write_an_outbox_message_that_gets_processed()
    {
        this.Given(x => x.A_pancake_batch_is_created_and_order_request_is_ready())
            .When(x => x.The_order_is_submitted())
            .Then(x => x.The_order_should_be_created_successfully())
            .And(x => x.An_outbox_message_should_have_been_written_and_processed())
            .BDDfy();
    }

    #region Steps

    private async Task A_pancake_batch_is_created_and_order_request_is_ready()
    {
        await _milkSteps.Retrieve();
        _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

        await _eggsSteps.Retrieve();
        _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

        await _flourSteps.Retrieve();
        _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();

        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        _pancakeSteps.Response.Should().NotBeNull();
        _pancakeSteps.Response!.BatchId.Should().NotBeEmpty();

        _orderSteps.Request.Items.Add(new TestOrderItemRequest
        {
            ItemType = OrderDefaults.PancakeItemType,
            BatchId = _pancakeSteps.Response!.BatchId,
            Quantity = 1
        });

        _orderSteps.Request.CustomerName = _customerName;
        _orderSteps.Request.TableNumber = 7;
    }

    private async Task The_order_is_submitted()
    {
        await _orderSteps.Send();
    }

    private async Task The_order_should_be_created_successfully()
    {
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        _orderSteps.Response!.CustomerName.Should().Be(_customerName);
        _orderSteps.Response!.Items.Should().HaveCount(1);
    }

    private async Task An_order_created_event_should_have_been_published()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

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

            orderCreatedEvents.Should().Contain(e => e.CustomerName == _customerName);
        }
    }

    private void The_kitchen_service_should_have_received_a_preparation_request()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
    }

    private async Task A_recipe_log_should_have_been_published_to_kafka()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

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

            recipeLogMessages.Should().Contain(m => m.Message.RecipeType == OrderDefaults.PancakeItemType,
                "a RecipeLogEvent should have been published for the pancake recipe");
        }
    }

    private async Task An_outbox_message_should_have_been_written_and_processed()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

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

        for (var i = 0; i < maxRetries; i++)
        {
            await OutboxSteps.LoadOutboxMessages();
            if (OutboxSteps.OutboxMessages!.Any(m => m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Processed))
                break;
            await Task.Delay(retryDelay);
        }

        OutboxSteps.AssertOutboxMessageWasProcessed(EventTypes.OrderCreated);
    }

    #endregion
}
