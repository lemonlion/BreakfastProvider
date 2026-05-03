using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.AuditLogs;

#pragma warning disable CS1998
public partial class AuditLogs__Filtering_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;

    private Guid _orderId;
    private HttpResponseMessage? _auditLogResponse;
    private List<TestAuditLogResponse>? _auditLogs;

    public AuditLogs__Filtering_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
    }

    #region Given

    private async Task<CompositeStep> An_order_has_been_created_to_generate_an_audit_log()
    {
        return Sub.Steps(
            _ => A_pancake_batch_is_created(),
            _ => An_order_is_created_for_the_batch());
    }

    private async Task A_pancake_batch_is_created()
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
    }

    private async Task An_order_is_created_for_the_batch()
    {
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

    #endregion

    #region When

    private async Task Audit_logs_are_requested_filtered_by_entity_type()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityType={AuditLogDefaults.OrderEntityType}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _auditLogResponse = await Client.SendAsync(request);
        var content = await _auditLogResponse.Content.ReadAsStringAsync();
        _auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(content)!;
    }

    private async Task Audit_logs_are_requested_filtered_by_entity_id()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityId={_orderId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _auditLogResponse = await Client.SendAsync(request);
        var content = await _auditLogResponse.Content.ReadAsStringAsync();
        _auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(content)!;
    }

    private async Task Audit_logs_are_requested_filtered_by_a_non_existent_entity_type()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityType=NonExistent_{Random.Shared.NextInt64()}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _auditLogResponse = await Client.SendAsync(request);
    }

    #endregion

    #region Then

    private async Task<CompositeStep> The_audit_log_response_should_only_contain_order_entries()
    {
        return Sub.Steps(
            _ => The_audit_log_response_status_should_be_ok(),
            _ => The_audit_logs_should_only_contain_order_entity_type());
    }

    private async Task<CompositeStep> The_audit_log_response_should_contain_the_specific_order_entry()
    {
        return Sub.Steps(
            _ => The_audit_log_response_status_should_be_ok(),
            _ => The_audit_logs_should_contain_the_created_order());
    }

    private async Task The_audit_log_response_status_should_be_ok()
        => Track.That(() => _auditLogResponse!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_audit_logs_should_only_contain_order_entity_type()
        => Track.That(() => _auditLogs!.Should().OnlyContain(l => l.EntityType == AuditLogDefaults.OrderEntityType));

    private async Task The_audit_logs_should_contain_the_created_order()
        => Track.That(() => _auditLogs!.Should().Contain(l => l.EntityId == _orderId));

    private async Task<CompositeStep> The_audit_log_response_should_be_an_empty_collection()
    {
        return Sub.Steps(
            _ => The_audit_log_response_status_should_be_ok(),
            _ => The_audit_logs_list_should_be_empty());
    }

    private async Task The_audit_logs_list_should_be_empty()
    {
        var content = await _auditLogResponse!.Content.ReadAsStringAsync();
        var auditLogsFromDifferentTimeRange = Json.Deserialize<List<TestAuditLogResponse>>(content)!;
        Track.That(() => auditLogsFromDifferentTimeRange.Should().BeEmpty());
    }

    private async Task The_audit_logs_should_be_ordered_by_timestamp_descending()
    {
        Track.That(() => _auditLogs.Should().NotBeNullOrEmpty());
        Track.That(() => _auditLogs!.Should().BeInDescendingOrder(l => l.Timestamp));
    }

    #endregion
}
