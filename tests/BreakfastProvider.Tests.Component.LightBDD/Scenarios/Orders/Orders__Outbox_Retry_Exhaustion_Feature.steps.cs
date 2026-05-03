using System.Net;
using BreakfastProvider.Api.Events.Outbox;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998
public partial class Orders__Outbox_Retry_Exhaustion_Feature : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private GetMilkSteps _milkSteps = null!;
    private GetEggsSteps _eggsSteps = null!;
    private GetFlourSteps _flourSteps = null!;
    private PostPancakesSteps _pancakeSteps = null!;
    private PostOrderSteps _orderSteps = null!;
    private OutboxSteps _outboxSteps = null!;

    public Orders__Outbox_Retry_Exhaustion_Feature() : base(delayAppCreation: true)
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

    #region Given

    private async Task<CompositeStep> A_pancake_batch_has_been_created()
    {
        return Sub.Steps(
            _ => Milk_is_retrieved(),
            _ => The_milk_response_should_be_successful(),
            _ => Eggs_are_retrieved(),
            _ => The_eggs_response_should_be_successful(),
            _ => Flour_is_retrieved(),
            _ => The_flour_response_should_be_successful(),
            _ => A_pancake_request_is_submitted(),
            _ => The_pancake_response_should_be_successful());
    }

    private async Task Milk_is_retrieved()
        => await _milkSteps.Retrieve();

    private async Task The_milk_response_should_be_successful()
        => Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Eggs_are_retrieved()
        => await _eggsSteps.Retrieve();

    private async Task The_eggs_response_should_be_successful()
        => Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Flour_is_retrieved()
        => await _flourSteps.Retrieve();

    private async Task The_flour_response_should_be_successful()
        => Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task A_pancake_request_is_submitted()
    {
        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
    }

    private async Task The_pancake_response_should_be_successful()
    {
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
        Track.That(() => _pancakeSteps.Response.Should().NotBeNull());
    }

    private async Task A_valid_order_request_for_the_created_batch()
    {
        _orderSteps.Request.CustomerName = _customerName;
        _orderSteps.Request.TableNumber = 7;
        _orderSteps.Request.Items.Add(new TestOrderItemRequest
        {
            ItemType = OrderDefaults.PancakeItemType,
            BatchId = _pancakeSteps.Response!.BatchId,
            Quantity = 1
        });
    }

    #endregion

    #region When

    private async Task The_breakfast_order_is_placed()
        => await _orderSteps.Send();

    #endregion

    #region Then

    private async Task The_order_should_be_created_successfully()
        => Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));

    private async Task The_outbox_message_should_transition_to_failed()
    {
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

    #endregion

    private class FailingOutboxDispatcher : IOutboxDispatcher
    {
        public string Destination => OutboxDestinations.EventGrid;

        public Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated dispatch failure for testing retry exhaustion.");
    }
}
