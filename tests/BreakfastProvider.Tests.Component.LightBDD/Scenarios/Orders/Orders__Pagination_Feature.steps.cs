using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998
public partial class Orders__Pagination_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly ListOrdersSteps _listSteps;

    private int _createdOrderCount;

    public Orders__Pagination_Feature() : base(delayAppCreation: true)
    {
        // Per-scenario factory ensures the empty-page test gets an isolated database
        // in in-memory mode; other scenarios are unaffected since they create their own data.
        CreateAppAndClient(additionalServices: _ => { });

        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _listSteps = Get<ListOrdersSteps>();
    }

    #region Given

    private async Task<CompositeStep> Multiple_orders_have_been_created()
    {
        return Sub.Steps(
            _ => A_pancake_batch_is_created(),
            _ => Two_orders_are_created_for_the_batch());
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
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
    }

    private async Task Two_orders_are_created_for_the_batch()
    {
        for (var i = 0; i < 2; i++)
        {
            _orderSteps.Request = new TestOrderRequest
            {
                CustomerName = $"PaginationTest_{Random.Shared.NextInt64()}",
                TableNumber = i + 1,
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
            Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        }

        _createdOrderCount = 2;
    }

    #endregion

    #region When

    private async Task Orders_are_listed_with_default_pagination()
    {
        await _listSteps.Retrieve();
    }

    private async Task Orders_are_listed_with_a_page_size_of_one()
    {
        await _listSteps.Retrieve(page: 1, pageSize: 1);
    }

    private async Task The_second_page_of_orders_is_requested_with_a_page_size_of_one()
    {
        await _listSteps.Retrieve(page: 2, pageSize: 1);
    }

    #endregion

    #region Then

    private async Task<CompositeStep> The_paginated_response_should_contain_the_correct_metadata()
    {
        return Sub.Steps(
            _ => The_list_response_should_be_ok(),
            _ => The_list_response_should_be_valid_json(),
            _ => The_response_should_contain_the_created_orders(),
            _ => The_page_number_should_be_one(),
            _ => The_total_count_should_match_the_created_order_count());
    }

    private async Task The_list_response_should_be_ok()
        => Track.That(() => _listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_list_response_should_be_valid_json()
        => await _listSteps.ParseResponse();

    private async Task The_response_should_contain_the_created_orders()
        => Track.That(() => _listSteps.Response!.Items.Should().HaveCountGreaterThanOrEqualTo(_createdOrderCount));

    private async Task The_page_number_should_be_one()
        => Track.That(() => _listSteps.Response!.Page.Should().Be(1));

    private async Task The_total_count_should_match_the_created_order_count()
        => Track.That(() => _listSteps.Response!.TotalCount.Should().BeGreaterThanOrEqualTo(_createdOrderCount));

    private async Task The_paginated_response_should_contain_only_one_item()
    {
        Track.That(() => _listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _listSteps.ParseResponse();
        Track.That(() => _listSteps.Response!.Items.Should().HaveCount(1));
    }

    private async Task The_total_pages_should_reflect_the_full_order_count()
        => Track.That(() => _listSteps.Response!.TotalPages.Should().BeGreaterThanOrEqualTo(_createdOrderCount));

    private async Task The_page_number_should_be_two()
        => Track.That(() => _listSteps.Response!.Page.Should().Be(2));

    private async Task<CompositeStep> The_paginated_response_should_be_empty()
    {
        return Sub.Steps(
            _ => The_list_response_should_be_ok(),
            _ => The_list_response_should_be_valid_json(),
            _ => The_items_list_should_be_empty(),
            _ => The_total_count_should_be_zero());
    }

    private async Task The_items_list_should_be_empty()
        => Track.That(() => _listSteps.Response!.Items.Should().BeEmpty());

    private async Task The_total_count_should_be_zero()
        => Track.That(() => _listSteps.Response!.TotalCount.Should().Be(0));

    #endregion
}
