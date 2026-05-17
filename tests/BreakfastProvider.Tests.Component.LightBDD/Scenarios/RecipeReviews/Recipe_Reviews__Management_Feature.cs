using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.RecipeReviews;

[FeatureDescription($"/{Endpoints.RecipeReviews} - Submitting and retrieving recipe reviews (MongoDB)")]
public partial class Recipe_Reviews__Management_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Submitting_A_Recipe_Review_Should_Return_The_Created_Review()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_recipe_review_request(),
            when => The_review_is_submitted(),
            then => The_review_response_should_contain_the_created_review());
    }

    [Scenario]
    public async Task Retrieving_Existing_Review_By_Id_Should_Return_The_Review()
    {
        await Runner.RunScenarioAsync(
            given => A_review_entry_exists(),
            when => The_review_is_retrieved_by_id(),
            then => The_review_get_response_should_contain_the_review());
    }

    [Scenario]
    public async Task Listing_Reviews_By_Recipe_Should_Return_Matching_Reviews()
    {
        await Runner.RunScenarioAsync(
            given => A_review_entry_exists(),
            when => The_reviews_are_listed_by_recipe(),
            then => The_review_list_response_should_contain_the_review());
    }

    [Scenario]
    public async Task Retrieving_Non_Existent_Review_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            when => A_non_existent_review_is_retrieved(),
            then => The_review_get_response_should_indicate_not_found());
    }

    [Scenario]
    public async Task Submitting_Review_With_Missing_Recipe_Name_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_review_request_with_missing_recipe_name(),
            when => The_review_is_submitted(),
            then => The_review_response_should_indicate_bad_request());
    }

    [Scenario]
    public async Task Submitting_Review_With_Invalid_Rating_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_review_request_with_an_invalid_rating(),
            when => The_review_is_submitted(),
            then => The_review_response_should_indicate_bad_request());
    }
}
