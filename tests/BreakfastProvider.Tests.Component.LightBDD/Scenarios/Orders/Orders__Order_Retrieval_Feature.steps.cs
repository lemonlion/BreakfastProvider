using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Orders__Order_Retrieval_Feature : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GetOrderSteps _retrievalSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Orders__Order_Retrieval_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _retrievalSteps = Get<GetOrderSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    private Guid _orderIdToRetrieve;

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
            TableNumber = 3,
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
        _orderIdToRetrieve = _orderSteps.Response.OrderId;
    }

    private async Task A_non_existent_order_id()
    {
        _orderIdToRetrieve = Guid.NewGuid();
    }

    #endregion

    #region When

    private async Task The_order_is_retrieved_by_id()
        => await _retrievalSteps.Retrieve(_orderIdToRetrieve);

    #endregion

    #region Then

    private async Task<CompositeStep> The_retrieved_order_should_match_the_created_order()
    {
        return Sub.Steps(
            _ => The_retrieval_response_http_status_should_be_ok(),
            _ => The_retrieval_response_should_be_valid_json(),
            _ => The_retrieved_order_id_should_match(),
            _ => The_retrieved_customer_name_should_match(),
            _ => The_retrieved_items_should_match());
    }

    private async Task The_retrieval_response_http_status_should_be_ok()
        => Track.That(() => _retrievalSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_retrieval_response_should_be_valid_json()
        => await _retrievalSteps.ParseResponse();

    private async Task The_retrieved_order_id_should_match()
        => Track.That(() => _retrievalSteps.Response!.OrderId.Should().Be(_orderSteps.Response!.OrderId));

    private async Task The_retrieved_customer_name_should_match()
        => Track.That(() => _retrievalSteps.Response!.CustomerName.Should().Be(_customerName));

    private async Task The_retrieved_items_should_match()
        => Track.That(() => _retrievalSteps.Response!.Items.Should().HaveCount(1));

    private async Task The_response_should_be_not_found()
        => Track.That(() => _retrievalSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_cow_service_should_have_received_a_milk_request()
        => _downstreamSteps.AssertCowServiceReceivedMilkRequest();

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_kitchen_service_should_have_received_a_preparation_request()
        => _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();

    #endregion
}
