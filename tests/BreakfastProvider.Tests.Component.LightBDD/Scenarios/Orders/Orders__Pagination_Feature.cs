using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Paginated listing of breakfast orders")]
public partial class Orders__Pagination_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Listing_Orders_Should_Return_A_Paginated_Response()
    {
        await Runner.RunScenarioAsync(
            given => Multiple_orders_have_been_created(),
            when => Orders_are_listed_with_default_pagination(),
            then => The_paginated_response_should_contain_the_correct_metadata());
    }

    [Scenario]
    public async Task Listing_Orders_With_A_Small_Page_Size_Should_Limit_Results()
    {
        await Runner.RunScenarioAsync(
            given => Multiple_orders_have_been_created(),
            when => Orders_are_listed_with_a_page_size_of_one(),
            then => The_paginated_response_should_contain_only_one_item(),
            and => The_total_pages_should_reflect_the_full_order_count());
    }

    [Scenario]
    public async Task Requesting_The_Second_Page_Should_Return_Different_Orders()
    {
        await Runner.RunScenarioAsync(
            given => Multiple_orders_have_been_created(),
            when => The_second_page_of_orders_is_requested_with_a_page_size_of_one(),
            then => The_paginated_response_should_contain_only_one_item(),
            and => The_page_number_should_be_two());
    }

    [Scenario]
    [IgnoreUnless(nameof(Settings.RunWithAnInMemoryDatabase), NeedsIsolatedDatabase)]
    public async Task Listing_Orders_When_None_Exist_Should_Return_An_Empty_Page()
    {
        await Runner.RunScenarioAsync(
            when => Orders_are_listed_with_default_pagination(),
            then => The_paginated_response_should_be_empty());
    }
}
