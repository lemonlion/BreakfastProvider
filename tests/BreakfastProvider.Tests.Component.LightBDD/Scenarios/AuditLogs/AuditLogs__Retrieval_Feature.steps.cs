using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.AuditLogs;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class AuditLogs__Retrieval_Feature : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GetAuditLogsSteps _auditSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public AuditLogs__Retrieval_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _auditSteps = Get<GetAuditLogsSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    #region Given

    private async Task<CompositeStep> A_pancake_batch_has_been_created()
    {
        return Sub.Steps(
            _ => A_pancake_request_is_submitted_with_ingredients(),
            _ => The_pancake_batch_response_should_be_successful());
    }

    private async Task A_pancake_request_is_submitted_with_ingredients()
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
    }

    private async Task The_pancake_batch_response_should_be_successful()
    {
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
        Track.That(() => _pancakeSteps.Response.Should().NotBeNull());
        Track.That(() => _pancakeSteps.Response!.BatchId.Should().NotBeEmpty());
    }

    private async Task<CompositeStep> An_order_has_been_created_for_the_batch()
    {
        return Sub.Steps(
            _ => An_order_request_is_submitted(),
            _ => The_order_creation_response_should_be_successful());
    }

    private async Task An_order_request_is_submitted()
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
    }

    private async Task The_order_creation_response_should_be_successful()
    {
        Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _orderSteps.ParseResponse();
        Track.That(() => _orderSteps.Response.Should().NotBeNull());
        Track.That(() => _orderSteps.Response!.OrderId.Should().NotBeEmpty());
    }

    #endregion

    #region When

    private async Task The_audit_logs_are_retrieved()
        => await _auditSteps.Retrieve();

    #endregion

    #region Then

    private async Task<CompositeStep> The_audit_log_response_should_contain_the_order_creation_entry()
    {
        return Sub.Steps(
            _ => The_audit_log_response_http_status_should_be_ok(),
            _ => The_audit_log_response_should_be_valid_json(),
            _ => The_audit_log_should_contain_an_order_created_entry());
    }

    private async Task The_audit_log_response_http_status_should_be_ok()
        => Track.That(() => _auditSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_audit_log_response_should_be_valid_json()
        => await _auditSteps.ParseResponse();

    private async Task The_audit_log_should_contain_an_order_created_entry()
    {
        Track.That(() => _auditSteps.Response!.Should().Contain(a =>
            a.Action == AuditLogDefaults.CreatedAction
            && a.EntityType == AuditLogDefaults.OrderEntityType
            && a.Details.Contains(_customerName)));
    }

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_cow_service_should_have_received_a_milk_request()
        => _downstreamSteps.AssertCowServiceReceivedMilkRequest();

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_kitchen_service_should_have_received_a_preparation_request()
        => _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();

    #endregion
}
