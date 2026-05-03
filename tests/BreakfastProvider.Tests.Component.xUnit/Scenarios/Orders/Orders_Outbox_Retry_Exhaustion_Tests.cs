using System.Net;
using BreakfastProvider.Api.Events.Outbox;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Orders;

public class Orders_Outbox_Retry_Exhaustion_Tests : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private GetMilkSteps _milkSteps = null!;
    private GetEggsSteps _eggsSteps = null!;
    private GetFlourSteps _flourSteps = null!;
    private PostPancakesSteps _pancakeSteps = null!;
    private PostOrderSteps _orderSteps = null!;
    private OutboxSteps _outboxSteps = null!;

    public Orders_Outbox_Retry_Exhaustion_Tests() : base(delayAppCreation: true)
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        CreateAppAndClient(
            configOverrides: new Dictionary<string, string?>
            {
                ["OutboxConfig:PollingIntervalSeconds"] = "1",
                ["OutboxConfig:MaxRetryCount"] = "2"
            },
            additionalServices: services =>
            {
                services.RemoveAll<IOutboxDispatcher>();
                services.AddSingleton<IOutboxDispatcher>(new FailingOutboxDispatcher());
            });

        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _outboxSteps = new OutboxSteps(AppFactory.Services.GetRequiredService<ICosmosRepository<OutboxMessage>>());
    }

    [Fact]
    public async Task Outbox_message_should_transition_to_failed_after_exhausting_retries()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;
        if (Settings.UsesSharedDockerDatabase) return;

        // Given a pancake batch has been created
        await _milkSteps.Retrieve();
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _eggsSteps.Retrieve();
        Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _flourSteps.Retrieve();
        Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
        Track.That(() => _pancakeSteps.Response.Should().NotBeNull());

        // And a valid order request for the created batch
        _orderSteps.Request.CustomerName = _customerName;
        _orderSteps.Request.TableNumber = 7;
        _orderSteps.Request.Items.Add(new TestOrderItemRequest
        {
            ItemType = OrderDefaults.PancakeItemType,
            BatchId = _pancakeSteps.Response!.BatchId,
            Quantity = 1
        });

        // When the breakfast order is placed
        await _orderSteps.Send();

        // Then the order should be created successfully
        Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));

        // And the outbox message should transition to failed
        const int maxRetries = 60;
        var retryDelay = TimeSpan.FromMilliseconds(500);

        for (var i = 0; i < maxRetries; i++)
        {
            await _outboxSteps.LoadOutboxMessages();
            if (_outboxSteps.OutboxMessages!.Any(m =>
                    m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Failed))
                return;
            await Task.Delay(retryDelay);
        }

        await _outboxSteps.LoadOutboxMessages();
        Track.That(() => _outboxSteps.OutboxMessages.Should().Contain(m =>
                m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Failed,
            "the outbox message should have transitioned to Failed after exhausting retries"));
    }

    private class FailingOutboxDispatcher : IOutboxDispatcher
    {
        public string Destination => OutboxDestinations.EventGrid;

        public Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated dispatch failure for testing retry exhaustion.");
    }
}
