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
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Orders;

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

    [Test]
    [HappyPath]
    public async Task Order_should_progress_through_all_status_transitions_to_completion()
    {
        // Given a pancake batch has been created
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

        // And a breakfast order has been placed for the batch
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
        await _orderSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        await _orderSteps.Response.Should().NotBeNull();
        _orderId = _orderSteps.Response!.OrderId;
        await _orderId.Should().NotBeEqualTo(Guid.Empty);

        // When the order is progressed through the complete lifecycle
        await _patchSteps.Send(_orderId, OrderStatuses.Preparing);
        await _patchSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _patchSteps.Send(_orderId, OrderStatuses.Ready);
        await _patchSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _patchSteps.Send(_orderId, OrderStatuses.Completed);
        await _patchSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        // Then the completed order should be retrievable with all details
        await _retrievalSteps.Retrieve(_orderId);
        await _retrievalSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _retrievalSteps.ParseResponse();
        await _retrievalSteps.Response!.Status.Should().BeEqualTo(OrderStatuses.Completed);
        await _retrievalSteps.Response!.CustomerName.Should().BeEqualTo(_customerName);
        await _retrievalSteps.Response!.Items.Should().HaveCount(1);
        await _retrievalSteps.Response!.TableNumber.Should().BeEqualTo(4);

        // And the order timestamps should be recent
        await (_retrievalSteps.Response!.CreatedAt > DateTime.UtcNow.AddMinutes(-2) && _retrievalSteps.Response!.CreatedAt < DateTime.UtcNow.AddMinutes(2)).Should().BeTrue();

        // And the order id should be a valid guid format
        await System.Text.RegularExpressions.Regex.IsMatch(_retrievalSteps.Response!.OrderId.ToString(), @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$").Should().BeTrue();

        // And an audit log entry should exist for the order
        var auditLogRequest = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityId={_orderId}");
        auditLogRequest.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        var auditLogResponse = await Client.SendAsync(auditLogRequest);
        await auditLogResponse.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        var auditContent = await auditLogResponse.Content.ReadAsStringAsync();
        var auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(auditContent)!;
        await auditLogs.Should().Contain(l => l.EntityId == _orderId && l.Action == AuditLogDefaults.CreatedAction);

        // And the cow service should have received a milk request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();

        // And the kitchen service should have received a preparation request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
    }
}
