using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.TabularAttributes;
using LightBDD.TabularAttributes.Attributes;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Waffles;

[FeatureDescription($"/{Endpoints.Waffles} - Creating waffles with ingredients and optional toppings")]
public partial class Waffles__Creation_Feature
{
    [HappyPath]
    [Scenario]
    public async Task A_Valid_Waffle_Request_Should_Return_A_Fresh_Batch()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_waffle_recipe_with_all_ingredients(),
            when => The_waffles_are_prepared(),
            then => The_waffles_response_should_contain_a_valid_batch_with_all_ingredients(),
            and => The_cow_service_should_have_received_a_milk_request());
    }

    #region Validation

    [Scenario]
    [HeadIn("Field",  "Value", "Reason"            )][HeadOut("Error Message",                 "Response Status")]
    [Inputs("Milk",   "",                      "Milk is required"  )][Outputs("'Milk' is required.",                                  "Bad Request"    )]
    [Inputs("Flour",  "",                      "Flour is required" )][Outputs("'Flour' is required.",                                 "Bad Request"    )]
    [Inputs("Eggs",   "",                      "Eggs is required"  )][Outputs("'Eggs' is required.",                                  "Bad Request"    )]
    [Inputs("Butter", "",                      "Butter is required")][Outputs("'Butter' is required.",                                "Bad Request"    )]
    [Inputs("Milk",   "<script>alert</script>", "XSS in milk"      )][Outputs("Milk contains potentially dangerous content.",         "Bad Request"    )]
    [Inputs("Butter", "<img onerror=x>",        "XSS in butter"    )][Outputs("Butter contains potentially dangerous content.",      "Bad Request"    )]
    public async Task Waffles_Endpoint_Is_Called_With_Invalid_Ingredients_Should_Return_A_Bad_Request_Response()
    {
        await Runner.RunScenarioAsync(
            given => Valid_waffle_requests_with_an_invalid_field(TableFrom.Inputs<InvalidFieldFromRequest>()),
            when => The_invalid_waffle_requests_are_submitted(),
            then => The_responses_should_each_contain_the_validation_error_for_the_invalid_field(VerifiableTableFrom.Outputs<VerifiableErrorResult>()));
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsNonDefaultConfiguration)]
    public async Task A_Waffle_Request_With_More_Toppings_Than_Allowed_Should_Return_A_Bad_Request_Response()
    {
        await Runner.RunScenarioAsync(
            given => The_max_toppings_per_item_is_LIMIT(MaxToppings),
            and => A_valid_waffle_recipe_with_all_ingredients(),
            and => The_request_has_more_toppings_than_the_configured_limit(),
            when => The_waffles_are_prepared(),
            then => The_waffles_response_should_indicate_too_many_toppings());
    }

    #endregion
}
