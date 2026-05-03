using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Orders;

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
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
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
            Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        }

        _createdOrderCount = 2;
    }

    [Fact]
    [HappyPath]
    public async Task Listing_orders_should_return_a_paginated_response()
    {
        // Given
        await CreateMultipleOrders();

        // When
        await _listSteps.Retrieve();

        // Then
        Track.That(() => _listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _listSteps.ParseResponse();
        Track.That(() => _listSteps.Response!.Items.Should().HaveCountGreaterThanOrEqualTo(_createdOrderCount));
        Track.That(() => _listSteps.Response!.Page.Should().Be(1));
        Track.That(() => _listSteps.Response!.TotalCount.Should().BeGreaterThanOrEqualTo(_createdOrderCount));
    }

    [Fact]
    public async Task Listing_orders_with_small_page_size_should_limit_results()
    {
        // Given
        await CreateMultipleOrders();

        // When
        await _listSteps.Retrieve(page: 1, pageSize: 1);

        // Then
        Track.That(() => _listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _listSteps.ParseResponse();
        Track.That(() => _listSteps.Response!.Items.Should().HaveCount(1));
        Track.That(() => _listSteps.Response!.TotalPages.Should().BeGreaterThanOrEqualTo(_createdOrderCount));
    }

    [Fact]
    public async Task Requesting_second_page_should_return_different_orders()
    {
        // Given
        await CreateMultipleOrders();

        // When
        await _listSteps.Retrieve(page: 2, pageSize: 1);

        // Then
        Track.That(() => _listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _listSteps.ParseResponse();
        Track.That(() => _listSteps.Response!.Items.Should().HaveCount(1));
        Track.That(() => _listSteps.Response!.Page.Should().Be(2));
    }

    [Fact]
    public async Task Listing_orders_when_none_exist_should_return_an_empty_page()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;
        if (!Settings.RunWithAnInMemoryDatabase) return;

        // When
        await _listSteps.Retrieve();

        // Then
        Track.That(() => _listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _listSteps.ParseResponse();
        Track.That(() => _listSteps.Response!.Items.Should().BeEmpty());
        Track.That(() => _listSteps.Response!.TotalCount.Should().Be(0));
    }
}
