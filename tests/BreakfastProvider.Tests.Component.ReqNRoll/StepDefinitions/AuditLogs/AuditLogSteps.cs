using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Util;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.AuditLogs;

[Binding]
public class AuditLogSteps(
    AppManager appManager,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps,
    GetAuditLogsSteps auditSteps)
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";
    private Guid _orderId;
    private HttpResponseMessage? _auditLogResponse;
    private List<TestAuditLogResponse>? _auditLogs;

    [Given("an order has been created for the batch")]
    public async Task GivenAnOrderHasBeenCreatedForTheBatch()
    {
        orderSteps.Request = new TestOrderRequest
        {
            CustomerName = _customerName,
            TableNumber = 5,
            Items =
            [
                new TestOrderItemRequest
                {
                    ItemType = OrderDefaults.PancakeItemType,
                    BatchId = pancakeSteps.Response!.BatchId,
                    Quantity = 1
                }
            ]
        };
        await orderSteps.Send();
        Track.That(() => orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await orderSteps.ParseResponse();
        Track.That(() => orderSteps.Response!.OrderId.Should().NotBeEmpty());
        _orderId = orderSteps.Response!.OrderId;
    }

    [When("the audit logs are retrieved")]
    public async Task WhenTheAuditLogsAreRetrieved() => await auditSteps.Retrieve();

    [When("audit logs are requested filtered by entity type")]
    public async Task WhenAuditLogsAreRequestedFilteredByEntityType()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityType={AuditLogDefaults.OrderEntityType}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _auditLogResponse = await appManager.Client.SendAsync(request);
        var content = await _auditLogResponse.Content.ReadAsStringAsync();
        _auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(content)!;
    }

    [When("audit logs are requested filtered by entity id")]
    public async Task WhenAuditLogsAreRequestedFilteredByEntityId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityId={_orderId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _auditLogResponse = await appManager.Client.SendAsync(request);
        var content = await _auditLogResponse.Content.ReadAsStringAsync();
        _auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(content)!;
    }

    [When("audit logs are requested filtered by a non-existent entity type")]
    public async Task WhenAuditLogsAreRequestedFilteredByANonExistentEntityType()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityType=NonExistent_{Random.Shared.NextInt64()}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _auditLogResponse = await appManager.Client.SendAsync(request);
    }

    [Then("the audit log response should contain the order creation entry")]
    public async Task ThenTheAuditLogResponseShouldContainTheOrderCreationEntry()
    {
        Track.That(() => auditSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await auditSteps.ParseResponse();
        Track.That(() => auditSteps.Response!.Should().Contain(a =>
            a.Action == AuditLogDefaults.CreatedAction
            && a.EntityType == AuditLogDefaults.OrderEntityType
            && a.Details.Contains(_customerName)));
    }

    [Then("the audit log response should only contain order entries")]
    public void ThenTheAuditLogResponseShouldOnlyContainOrderEntries()
    {
        Track.That(() => _auditLogResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
        Track.That(() => _auditLogs!.Should().OnlyContain(l => l.EntityType == AuditLogDefaults.OrderEntityType));
    }

    [Then("the audit log response should contain the specific order entry")]
    public void ThenTheAuditLogResponseShouldContainTheSpecificOrderEntry()
    {
        Track.That(() => _auditLogResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
        Track.That(() => _auditLogs!.Should().Contain(l => l.EntityId == _orderId));
    }

    [Then("the audit log response should be an empty collection")]
    public async Task ThenTheAuditLogResponseShouldBeAnEmptyCollection()
    {
        Track.That(() => _auditLogResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
        var content = await _auditLogResponse.Content.ReadAsStringAsync();
        var auditLogsFromDifferentTimeRange = Json.Deserialize<List<TestAuditLogResponse>>(content)!;
        Track.That(() => auditLogsFromDifferentTimeRange.Should().BeEmpty());
    }

    [Then("the audit logs should be ordered by timestamp descending")]
    public void ThenTheAuditLogsShouldBeOrderedByTimestampDescending()
    {
        Track.That(() => _auditLogs.Should().NotBeNullOrEmpty());
        Track.That(() => _auditLogs!.Should().BeInDescendingOrder(l => l.Timestamp));
    }
}
