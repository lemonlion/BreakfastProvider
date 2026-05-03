using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Util;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.AuditLogs;

public class AuditLogs_Filtering_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;

    private Guid _orderId;
    private HttpResponseMessage? _auditLogResponse;
    private List<TestAuditLogResponse>? _auditLogs;

    public AuditLogs_Filtering_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
    }

    private async Task CreateOrderToGenerateAuditLog()
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
        await _pancakeSteps.ParseResponse();

        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"AuditTestCustomer_{Random.Shared.NextInt64()}",
            Items = [new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = _pancakeSteps.Response!.BatchId, Quantity = 1 }],
            TableNumber = 3
        };
        await _orderSteps.Send();
        await _orderSteps.ParseResponse();
        _orderId = _orderSteps.Response!.OrderId;
    }

    [Fact]
    public async Task Audit_logs_should_be_filterable_by_entity_type()
    {
        // Given an order has been created to generate an audit log
        await CreateOrderToGenerateAuditLog();

        // When audit logs are requested filtered by entity type
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityType={AuditLogDefaults.OrderEntityType}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _auditLogResponse = await Client.SendAsync(request);
        var content = await _auditLogResponse.Content.ReadAsStringAsync();
        _auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(content)!;

        // Then the audit log response should only contain order entries
        Track.That(() => _auditLogResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
        Track.That(() => _auditLogs!.Should().OnlyContain(l => l.EntityType == AuditLogDefaults.OrderEntityType));
    }

    [Fact]
    public async Task Audit_logs_should_be_filterable_by_entity_id()
    {
        // Given an order has been created to generate an audit log
        await CreateOrderToGenerateAuditLog();

        // When audit logs are requested filtered by entity ID
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityId={_orderId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _auditLogResponse = await Client.SendAsync(request);
        var content = await _auditLogResponse.Content.ReadAsStringAsync();
        _auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(content)!;

        // Then the audit log response should contain the specific order entry
        Track.That(() => _auditLogResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
        Track.That(() => _auditLogs!.Should().Contain(l => l.EntityId == _orderId));
    }

    [Fact]
    public async Task Filtering_audit_logs_by_a_non_existent_entity_type_should_return_an_empty_collection()
    {
        // When audit logs are requested filtered by a non-existent entity type
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityType=NonExistent_{Random.Shared.NextInt64()}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _auditLogResponse = await Client.SendAsync(request);

        // Then the audit log response should be an empty collection
        Track.That(() => _auditLogResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
        var content = await _auditLogResponse!.Content.ReadAsStringAsync();
        var auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(content)!;
        Track.That(() => auditLogs.Should().BeEmpty());
    }

    [Fact]
    public async Task Audit_logs_should_be_returned_in_descending_timestamp_order()
    {
        // Given an order has been created to generate an audit log
        await CreateOrderToGenerateAuditLog();

        // When audit logs are requested filtered by entity type
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityType={AuditLogDefaults.OrderEntityType}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _auditLogResponse = await Client.SendAsync(request);
        var content = await _auditLogResponse.Content.ReadAsStringAsync();
        _auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(content)!;

        // Then the audit logs should be ordered by timestamp descending
        Track.That(() => _auditLogs.Should().NotBeNullOrEmpty());
        Track.That(() => _auditLogs!.Should().BeInDescendingOrder(l => l.Timestamp));
    }
}
