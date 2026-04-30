using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Fakes.EventGrid;
using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Models.Events;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class BreakfastOrderSteps(
    AppManager appManager,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps)
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    [Given("a valid order request for the created batch")]
    public void GivenAValidOrderRequestForTheCreatedBatch()
    {
        orderSteps.Request.CustomerName = _customerName;
        orderSteps.Request.TableNumber = 7;
        orderSteps.Request.Items.Add(new TestOrderItemRequest
        {
            ItemType = OrderDefaults.PancakeItemType,
            BatchId = pancakeSteps.Response!.BatchId,
            Quantity = 1
        });
    }

    [When("the breakfast order is placed")]
    public async Task WhenTheBreakfastOrderIsPlaced()
    {
        await orderSteps.Send();
    }

    [Then("the order response should contain a complete order")]
    public async Task ThenTheOrderResponseShouldContainACompleteOrder()
    {
        orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await orderSteps.ParseResponse();
        orderSteps.Response!.CustomerName.Should().Be(_customerName);
        orderSteps.Response!.Items.Should().HaveCount(1);
    }

    [Then("an order created event should have been published")]
    public async Task ThenAnOrderCreatedEventShouldHaveBeenPublished()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;

        var eventStore = appManager.AppFactory.Services.GetService<IPublishedEventStore>();
        eventStore.Should().NotBeNull();

        const int maxRetries = 100;
        var retryDelay = TimeSpan.FromMilliseconds(300);

        IReadOnlyList<TestOrderCreatedEvent> events = [];
        for (var i = 0; i < maxRetries; i++)
        {
            events = await eventStore!.GetPublishedEventsAsync<TestOrderCreatedEvent>();
            if (events.Any(e => e.CustomerName == _customerName))
                return;
            await Task.Delay(retryDelay);
        }

        events.Should().Contain(e => e.CustomerName == _customerName);
    }

    [Then("a recipe log should have been published to kafka")]
    public async Task ThenARecipeLogShouldHaveBeenPublishedToKafka()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;

        var kafkaStore = appManager.AppFactory.Services.GetService<IKafkaMessageStore>();
        kafkaStore.Should().NotBeNull();

        const int maxRetries = 50;
        var retryDelay = TimeSpan.FromMilliseconds(200);

        IReadOnlyList<(string Key, TestRecipeLogEvent Message)> messages = [];
        for (var i = 0; i < maxRetries; i++)
        {
            messages = kafkaStore!.GetMessages<TestRecipeLogEvent>();
            if (messages.Any(m => m.Message.RecipeType == OrderDefaults.PancakeItemType))
                return;
            await Task.Delay(retryDelay);
        }

        messages.Should().Contain(m => m.Message.RecipeType == OrderDefaults.PancakeItemType,
            "a RecipeLogEvent should have been published for the pancake recipe");
    }

    [Then("an outbox message should have been written for the order created event")]
    public async Task ThenAnOutboxMessageShouldHaveBeenWrittenForTheOrderCreatedEvent()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;

        var outboxSteps = new OutboxSteps(
            appManager.AppFactory.Services.GetRequiredService<Api.Storage.ICosmosRepository<Api.Storage.OutboxMessage>>());

        const int maxRetries = 50;
        var retryDelay = TimeSpan.FromMilliseconds(200);

        for (var i = 0; i < maxRetries; i++)
        {
            await outboxSteps.LoadOutboxMessages();
            if (outboxSteps.OutboxMessages!.Any(m => m.EventType == EventTypes.OrderCreated))
                break;
            await Task.Delay(retryDelay);
        }

        outboxSteps.AssertOutboxContainsMessageForEventType(EventTypes.OrderCreated);
    }

    [Then("the outbox message should have been processed")]
    public async Task ThenTheOutboxMessageShouldHaveBeenProcessed()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;

        var outboxSteps = new OutboxSteps(
            appManager.AppFactory.Services.GetRequiredService<Api.Storage.ICosmosRepository<Api.Storage.OutboxMessage>>());

        const int maxRetries = 50;
        var retryDelay = TimeSpan.FromMilliseconds(200);

        for (var i = 0; i < maxRetries; i++)
        {
            await outboxSteps.LoadOutboxMessages();
            if (outboxSteps.OutboxMessages!.Any(m => m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Processed))
                return;
            await Task.Delay(retryDelay);
        }

        outboxSteps.AssertOutboxMessageWasProcessed(EventTypes.OrderCreated);
    }
}
