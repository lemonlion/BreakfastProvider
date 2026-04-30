using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998
public partial class Orders__Complete_Lifecycle_Feature : BaseFixture
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

    public Orders__Complete_Lifecycle_Feature()
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

    #region Given

    private async Task<CompositeStep> A_pancake_batch_has_been_created()
    {
        return Sub.Steps(
            _ => Milk_is_retrieved_from_the_milk_endpoint(),
            _ => The_milk_response_should_be_successful(),
            _ => Eggs_are_retrieved_from_the_eggs_endpoint(),
            _ => The_eggs_response_should_be_successful(),
            _ => Flour_is_retrieved_from_the_flour_endpoint(),
            _ => The_flour_response_should_be_successful(),
            _ => A_pancake_request_is_submitted_with_all_ingredients(),
            _ => The_pancake_batch_response_should_be_successful());
    }

    private async Task Milk_is_retrieved_from_the_milk_endpoint()
        => await _milkSteps.Retrieve();

    private async Task The_milk_response_should_be_successful()
        => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task Eggs_are_retrieved_from_the_eggs_endpoint()
        => await _eggsSteps.Retrieve();

    private async Task The_eggs_response_should_be_successful()
        => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task Flour_is_retrieved_from_the_flour_endpoint()
        => await _flourSteps.Retrieve();

    private async Task The_flour_response_should_be_successful()
        => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task A_pancake_request_is_submitted_with_all_ingredients()
    {
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
        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        _pancakeSteps.Response.Should().NotBeNull();
        _pancakeSteps.Response!.BatchId.Should().NotBeEmpty();
    }

    private async Task<CompositeStep> A_breakfast_order_has_been_placed_for_the_batch()
    {
        return Sub.Steps(
            _ => An_order_request_is_submitted_for_the_pancake_batch(),
            _ => The_order_creation_response_should_be_successful(),
            _ => The_order_id_is_captured_from_the_order_response());
    }

    private async Task An_order_request_is_submitted_for_the_pancake_batch()
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
    }

    private async Task The_order_creation_response_should_be_successful()
    {
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        _orderSteps.Response.Should().NotBeNull();
    }

    private async Task The_order_id_is_captured_from_the_order_response()
    {
        _orderId = _orderSteps.Response!.OrderId;
        _orderId.Should().NotBeEmpty();
    }

    #endregion

    #region When

    private async Task<CompositeStep> The_order_is_progressed_through_the_complete_lifecycle()
    {
        return Sub.Steps(
            _ => The_order_status_is_updated_to_preparing(),
            _ => The_preparing_transition_should_succeed(),
            _ => The_order_status_is_updated_to_ready(),
            _ => The_ready_transition_should_succeed(),
            _ => The_order_status_is_updated_to_completed(),
            _ => The_completed_transition_should_succeed());
    }

    private async Task The_order_status_is_updated_to_preparing()
        => await _patchSteps.Send(_orderId, OrderStatuses.Preparing);

    private async Task The_preparing_transition_should_succeed()
        => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_order_status_is_updated_to_ready()
        => await _patchSteps.Send(_orderId, OrderStatuses.Ready);

    private async Task The_ready_transition_should_succeed()
        => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_order_status_is_updated_to_completed()
        => await _patchSteps.Send(_orderId, OrderStatuses.Completed);

    private async Task The_completed_transition_should_succeed()
        => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    #endregion

    #region Then

    private async Task<CompositeStep> The_completed_order_should_be_retrievable_with_all_details()
    {
        return Sub.Steps(
            _ => The_order_is_retrieved_by_id(),
            _ => The_retrieval_response_should_be_ok(),
            _ => The_retrieved_order_should_be_valid_json(),
            _ => The_retrieved_order_status_should_be_completed(),
            _ => The_retrieved_customer_name_should_match(),
            _ => The_retrieved_items_should_match(),
            _ => The_retrieved_table_number_should_match());
    }

    private async Task The_order_is_retrieved_by_id()
        => await _retrievalSteps.Retrieve(_orderId);

    private async Task The_retrieval_response_should_be_ok()
        => _retrievalSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_retrieved_order_should_be_valid_json()
        => await _retrievalSteps.ParseResponse();

    private async Task The_retrieved_order_status_should_be_completed()
        => _retrievalSteps.Response!.Status.Should().Be(OrderStatuses.Completed);

    private async Task The_retrieved_customer_name_should_match()
        => _retrievalSteps.Response!.CustomerName.Should().Be(_customerName);

    private async Task The_retrieved_items_should_match()
        => _retrievalSteps.Response!.Items.Should().HaveCount(1);

    private async Task The_retrieved_table_number_should_match()
        => _retrievalSteps.Response!.TableNumber.Should().Be(4);

    private async Task The_order_timestamps_should_be_recent()
        => _retrievalSteps.Response!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(2));

    private async Task The_order_id_should_be_a_valid_guid_format()
        => _retrievalSteps.Response!.OrderId.ToString().Should().MatchRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");

    private async Task<CompositeStep> An_audit_log_entry_should_exist_for_the_order()
    {
        return Sub.Steps(
            _ => Audit_logs_are_retrieved_for_the_order(),
            _ => The_audit_logs_should_contain_the_order_creation_entry());
    }

    private HttpResponseMessage? _auditLogResponse;
    private List<Models.AuditLogs.TestAuditLogResponse>? _auditLogs;

    private async Task Audit_logs_are_retrieved_for_the_order()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.AuditLogs}?entityId={_orderId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _auditLogResponse = await Client.SendAsync(request);
        _auditLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await _auditLogResponse.Content.ReadAsStringAsync();
        _auditLogs = Json.Deserialize<List<Models.AuditLogs.TestAuditLogResponse>>(content)!;
    }

    private async Task The_audit_logs_should_contain_the_order_creation_entry()
        => _auditLogs!.Should().Contain(l => l.EntityId == _orderId && l.Action == AuditLogDefaults.CreatedAction);

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_cow_service_should_have_received_a_milk_request()
        => _downstreamSteps.AssertCowServiceReceivedMilkRequest();

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_kitchen_service_should_have_received_a_preparation_request()
        => _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();

    #endregion
}
