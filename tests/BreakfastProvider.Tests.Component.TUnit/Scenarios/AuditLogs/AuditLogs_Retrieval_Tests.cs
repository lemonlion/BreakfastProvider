using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Kronikol.Tracking;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.AuditLogs;

public class AuditLogs_Retrieval_Tests : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GetAuditLogsSteps _auditSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public AuditLogs_Retrieval_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _auditSteps = Get<GetAuditLogsSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Creating_an_order_should_produce_a_retrievable_audit_log_entry()
    {
        await A_pancake_batch_has_been_created();
        await An_order_has_been_created_for_the_batch();
        await The_audit_logs_are_retrieved();
        await The_response_contains_the_order_creation_entry();
        The_downstream_services_received_requests();
    }

    [GivenStep] // Completely optional - you only need to add this if you want explicit steps in your reports
    private async Task A_pancake_batch_has_been_created()
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
        await _pancakeSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        await _pancakeSteps.Response.Should().NotBeNull();
        await _pancakeSteps.Response!.BatchId.Should().NotBeEqualTo(Guid.Empty);
    }

    [GivenStep] // Completely optional - you only need to add this if you want explicit steps in your reports
    private async Task An_order_has_been_created_for_the_batch()
    {
        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = _customerName,
            TableNumber = 5,
            Items =
            [
                new TestOrderItemRequest
                {
                    ItemType = OrderDefaults.PancakeItemType,
                    BatchId = _pancakeSteps.Response!.BatchId,
                    Quantity = 1
                }
            ]
        };
        await _orderSteps.Send();
        await _orderSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        await _orderSteps.Response.Should().NotBeNull();
        await _orderSteps.Response!.OrderId.Should().NotBeEqualTo(Guid.Empty);
    }

    [WhenStep] // Completely optional - you only need to add this if you want explicit steps in your reports
    private async Task The_audit_logs_are_retrieved()
    {
        await _auditSteps.Retrieve();
    }

    [ThenStep] // Completely optional - you only need to add this if you want explicit steps in your reports
    private async Task The_response_contains_the_order_creation_entry()
    {
        await _auditSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _auditSteps.ParseResponse();
        await _auditSteps.Response!.Should().Contain(a =>
            a.Action == AuditLogDefaults.CreatedAction
            && a.EntityType == AuditLogDefaults.OrderEntityType
            && a.Details.Contains(_customerName));
    }

    [ThenStep] // Completely optional - you only need to add this if you want explicit steps in your reports
    private void The_downstream_services_received_requests()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
        {
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
            _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
        }
    }
}
