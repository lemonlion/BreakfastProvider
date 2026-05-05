using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reporting;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Reporting__Order_Summaries_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GraphQlReportingSteps _graphQlSteps;
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";
    private Guid _orderId;

    public Reporting__Order_Summaries_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _graphQlSteps = Get<GraphQlReportingSteps>();
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
            TableNumber = 7,
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

    private async Task The_order_summaries_are_queried_via_graphql()
        => await _graphQlSteps.QueryOrderSummaries();

    #endregion

    #region Then

    private async Task<CompositeStep> The_graphql_response_should_contain_the_ingested_order_summary()
    {
        return Sub.Steps(
            _ => The_graphql_response_should_be_successful(),
            _ => The_order_summaries_response_should_be_valid_json(),
            _ => The_order_summaries_should_contain_the_test_order());
    }

    private async Task The_graphql_response_should_be_successful()
        => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_order_summaries_response_should_be_valid_json()
        => await _graphQlSteps.ParseOrderSummariesResponse();

    private async Task The_order_summaries_should_contain_the_test_order()
    {
        _graphQlSteps.OrderSummaries.Should().Contain(o =>
            o.OrderId == _orderId &&
            o.CustomerName == _customerName &&
            o.ItemCount == 1 &&
            o.TableNumber == 7);
    }

    private async Task The_order_summaries_list_should_be_empty_or_not_contain_the_test_order()
    {
        await _graphQlSteps.ParseOrderSummariesResponse();
        _graphQlSteps.OrderSummaries.Should().NotContain(o => o.OrderId == _orderId);
    }

    #endregion
}
