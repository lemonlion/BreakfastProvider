using System.Net;
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
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.AuditLogs;

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

    [Fact]
    public void Audit_logs_should_be_filterable_by_entity_type()
    {
        this.Given(x => x.An_order_has_been_created_to_generate_an_audit_log())
            .When(x => x.Audit_logs_are_requested_filtered_by_entity_type())
            .Then(x => x.The_audit_log_response_should_be_OK())
            .And(x => x.The_audit_logs_should_only_contain_order_entries())
            .BDDfy();
    }

    [Fact]
    public void Audit_logs_should_be_filterable_by_entity_id()
    {
        this.Given(x => x.An_order_has_been_created_to_generate_an_audit_log())
            .When(x => x.Audit_logs_are_requested_filtered_by_entity_id())
            .Then(x => x.The_audit_log_response_should_be_OK())
            .And(x => x.The_audit_logs_should_contain_the_specific_order_entry())
            .BDDfy();
    }

    [Fact]
    public void Filtering_audit_logs_by_a_non_existent_entity_type_should_return_an_empty_collection()
    {
        this.When(x => x.Audit_logs_are_requested_filtered_by_a_non_existent_entity_type())
            .Then(x => x.The_audit_log_response_should_be_OK())
            .And(x => x.The_audit_logs_should_be_empty())
            .BDDfy();
    }

    [Fact]
    public void Audit_logs_should_be_returned_in_descending_timestamp_order()
    {
        this.Given(x => x.An_order_has_been_created_to_generate_an_audit_log())
            .When(x => x.Audit_logs_are_requested_filtered_by_entity_type())
            .Then(x => x.The_audit_logs_should_be_ordered_by_timestamp_descending())
            .BDDfy();
    }

    #region Steps

    private async Task An_order_has_been_created_to_generate_an_audit_log()
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
        var content = await _auditLogResponse.Content.ReadAsStringAsync();
        _auditLogs = Json.Deserialize<List<TestAuditLogResponse>>(content)!;
    }

    private void The_audit_log_response_should_be_OK()
        => _auditLogResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

    private void The_audit_logs_should_only_contain_order_entries()
        => _auditLogs!.Should().OnlyContain(l => l.EntityType == AuditLogDefaults.OrderEntityType);

    private void The_audit_logs_should_contain_the_specific_order_entry()
        => _auditLogs!.Should().Contain(l => l.EntityId == _orderId);

    private void The_audit_logs_should_be_empty()
        => _auditLogs!.Should().BeEmpty();

    private void The_audit_logs_should_be_ordered_by_timestamp_descending()
    {
        _auditLogs.Should().NotBeNullOrEmpty();
        _auditLogs!.Should().BeInDescendingOrder(l => l.Timestamp);
    }

    #endregion
}
