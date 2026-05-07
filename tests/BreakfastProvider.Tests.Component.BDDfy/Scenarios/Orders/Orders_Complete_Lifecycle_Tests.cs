using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Util;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Orders;

public class Orders_Complete_Lifecycle_Tests : BaseFixture
{
    private readonly string _customerName = $"LifecycleTestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GetOrderSteps _retrievalSteps;
    private readonly PatchOrderStatusSteps _patchSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    private Guid _orderId;

    public Orders_Complete_Lifecycle_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _retrievalSteps = Get<GetOrderSteps>();
        _patchSteps = Get<PatchOrderStatusSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public void Order_should_progress_through_all_status_transitions_to_completion()
    {
        this.Given(x => x.A_pancake_batch_has_been_created())
            .And(x => x.A_breakfast_order_has_been_placed_for_the_batch())
            .When(x => x.The_order_is_progressed_through_the_complete_lifecycle())
            .Then(x => x.The_completed_order_should_be_retrievable_with_all_details())
            .And(x => x.An_audit_log_entry_should_exist_for_the_order())
            .And(x => x.The_downstream_services_should_have_received_requests())
            .BDDfy();
    }

    #region Steps

    private async Task A_pancake_batch_has_been_created()
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
    }

    private async Task A_breakfast_order_has_been_placed_for_the_batch()
    {
        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = _customerName,
            TableNumber = 4,
            Items =
            [
                new TestOrderItemRequest
                {
                    ItemType = OrderDefaults.PancakeItemType,
                    BatchId = _pancakeSteps.Response!.BatchId,
                    Quantity = 2
                }
            ]
        };
        await _orderSteps.Send();
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        _orderSteps.Response.Should().NotBeNull();
        _orderId = _orderSteps.Response!.OrderId;
        _orderId.Should().NotBeEmpty();
    }

    private async Task The_order_is_progressed_through_the_complete_lifecycle()
    {
        await _patchSteps.Send(_orderId, OrderStatuses.Preparing);
        _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _patchSteps.Send(_orderId, OrderStatuses.Ready);
        _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _patchSteps.Send(_orderId, OrderStatuses.Completed);
        _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task The_completed_order_should_be_retrievable_with_all_details()
    {
        await _retrievalSteps.Retrieve(_orderId);
        _retrievalSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _retrievalSteps.ParseResponse();
        _retrievalSteps.Response!.Status.Should().Be(OrderStatuses.Completed);
        _retrievalSteps.Response!.CustomerName.Should().Be(_customerName);
        _retrievalSteps.Response!.Items.Should().HaveCount(1);
        _retrievalSteps.Response!.TableNumber.Should().Be(4);
        _retrievalSteps.Response!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(2));
        _retrievalSteps.Response!.OrderId.ToString().Should().MatchRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");
    }

    private async Task An_audit_log_entry_should_exist_for_the_order()
    {
        var auditLogRequest = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityId={_orderId}");
        auditLogRequest.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        var auditLogResponse = await Client.SendAsync(auditLogRequest);
        auditLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auditContent = await auditLogResponse.Content.ReadAsStringAsync();
        var auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(auditContent)!;
        auditLogs.Should().Contain(l => l.EntityId == _orderId && l.Action == AuditLogDefaults.CreatedAction);
    }

    private void The_downstream_services_should_have_received_requests()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
        {
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
            _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
        }
    }

    #endregion
}
