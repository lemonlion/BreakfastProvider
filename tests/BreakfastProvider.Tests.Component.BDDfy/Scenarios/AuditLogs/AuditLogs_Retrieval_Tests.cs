using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.AuditLogs;

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

    [Fact]
    [HappyPath]
    public void Creating_an_order_should_produce_a_retrievable_audit_log_entry()
    {
        this.Given(x => x.A_pancake_batch_has_been_created())
            .And(x => x.An_order_has_been_created_for_the_batch())
            .When(x => x.The_audit_logs_are_retrieved())
            .Then(x => x.The_audit_log_response_should_contain_the_order_creation_entry())
            .And(x => x.The_downstream_services_should_have_received_requests())
            .BDDfy();
    }

    #region Steps

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
        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        _pancakeSteps.Response.Should().NotBeNull();
        _pancakeSteps.Response!.BatchId.Should().NotBeEmpty();
    }

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
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        _orderSteps.Response.Should().NotBeNull();
        _orderSteps.Response!.OrderId.Should().NotBeEmpty();
    }

    private async Task The_audit_logs_are_retrieved()
    {
        await _auditSteps.Retrieve();
    }

    private async Task The_audit_log_response_should_contain_the_order_creation_entry()
    {
        _auditSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _auditSteps.ParseResponse();
        _auditSteps.Response!.Should().Contain(a =>
            a.Action == AuditLogDefaults.CreatedAction
            && a.EntityType == AuditLogDefaults.OrderEntityType
            && a.Details.Contains(_customerName));
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
