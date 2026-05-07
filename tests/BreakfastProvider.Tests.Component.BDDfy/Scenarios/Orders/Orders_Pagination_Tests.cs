using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Orders;

public class Orders_Pagination_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly ListOrdersSteps _listSteps;

    private int _createdOrderCount;

    public Orders_Pagination_Tests() : base(delayAppCreation: true)
    {
        CreateAppAndClient(additionalServices: _ => { });

        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _listSteps = Get<ListOrdersSteps>();
    }

    private async Task CreateMultipleOrders()
    {
        // Create a pancake batch
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

        // Create two orders
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
            _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        _createdOrderCount = 2;
    }

    [Fact]
    [HappyPath]
    public void Listing_orders_should_return_a_paginated_response()
    {
        this.Given(x => x.Multiple_orders_have_been_created())
            .When(x => x.The_orders_are_listed())
            .Then(x => x.The_response_should_contain_a_paginated_list_of_orders())
            .BDDfy();
    }

    [Fact]
    public void Listing_orders_with_small_page_size_should_limit_results()
    {
        this.Given(x => x.Multiple_orders_have_been_created())
            .When(x => x.The_orders_are_listed_with_page_size_1())
            .Then(x => x.The_response_should_contain_only_one_item_per_page())
            .BDDfy();
    }

    [Fact]
    public void Requesting_second_page_should_return_different_orders()
    {
        this.Given(x => x.Multiple_orders_have_been_created())
            .When(x => x.The_second_page_is_requested_with_page_size_1())
            .Then(x => x.The_response_should_be_for_page_2())
            .BDDfy();
    }

    [Fact]
    public void Listing_orders_when_none_exist_should_return_an_empty_page()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;
        if (!Settings.RunWithAnInMemoryDatabase) return;

        this.When(x => x.The_orders_are_listed())
            .Then(x => x.The_response_should_be_an_empty_page())
            .BDDfy();
    }

    #region Steps

    private async Task Multiple_orders_have_been_created()
    {
        await CreateMultipleOrders();
    }

    private async Task The_orders_are_listed()
    {
        await _listSteps.Retrieve();
    }

    private async Task The_orders_are_listed_with_page_size_1()
    {
        await _listSteps.Retrieve(page: 1, pageSize: 1);
    }

    private async Task The_second_page_is_requested_with_page_size_1()
    {
        await _listSteps.Retrieve(page: 2, pageSize: 1);
    }

    private async Task The_response_should_contain_a_paginated_list_of_orders()
    {
        _listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _listSteps.ParseResponse();
        _listSteps.Response!.Items.Should().HaveCountGreaterThanOrEqualTo(_createdOrderCount);
        _listSteps.Response!.Page.Should().Be(1);
        _listSteps.Response!.TotalCount.Should().BeGreaterThanOrEqualTo(_createdOrderCount);
    }

    private async Task The_response_should_contain_only_one_item_per_page()
    {
        _listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _listSteps.ParseResponse();
        _listSteps.Response!.Items.Should().HaveCount(1);
        _listSteps.Response!.TotalPages.Should().BeGreaterThanOrEqualTo(_createdOrderCount);
    }

    private async Task The_response_should_be_for_page_2()
    {
        _listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _listSteps.ParseResponse();
        _listSteps.Response!.Items.Should().HaveCount(1);
        _listSteps.Response!.Page.Should().Be(2);
    }

    private async Task The_response_should_be_an_empty_page()
    {
        _listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _listSteps.ParseResponse();
        _listSteps.Response!.Items.Should().BeEmpty();
        _listSteps.Response!.TotalCount.Should().Be(0);
    }

    #endregion
}
