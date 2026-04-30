using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.TabularAttributes;
using LightBDD.TabularAttributes.Attributes;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Toppings;

[FeatureDescription($"/{Endpoints.Toppings} - Updating existing toppings")]
public partial class Toppings__Update_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Updating_An_Existing_Topping_Should_Return_The_Updated_Topping()
    {
        await Runner.RunScenarioAsync(
            given => A_known_topping_exists(),
            and => A_valid_update_topping_request(),
            when => The_topping_is_updated(),
            then => The_update_response_should_contain_the_updated_topping());
    }

    [Scenario]
    public async Task Updating_A_Non_Existent_Topping_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            given => A_topping_id_that_does_not_exist(),
            and => A_valid_update_topping_request(),
            when => The_topping_is_updated(),
            then => The_update_response_should_indicate_not_found());
    }

    #region Validation

    [Scenario]
    [HeadIn("Field",    "Value",                                  "Reason"                      )][HeadOut("Error Message",                                     "Response Status")]
    [Inputs("Name",     "<script>alert('xss')</script>",          "Script tag in name"          )][Outputs("Name contains potentially dangerous content.",      "Bad Request"    )]
    [Inputs("Name",     "<img src=x onerror=alert(1)>",           "Event handler in name"       )][Outputs("Name contains potentially dangerous content.",      "Bad Request"    )]
    [Inputs("Category", "<script>alert('xss')</script>",          "Script tag in category"      )][Outputs("Category contains potentially dangerous content.",  "Bad Request"    )]
    [Inputs("Category", "javascript:alert(1)",                    "Javascript protocol"         )][Outputs("Category contains potentially dangerous content.",  "Bad Request"    )]
    [Inputs("Name",     "",                                       "Name is required"            )][Outputs("'Name' is required.",                              "Bad Request"    )]
    [Inputs("Category", "",                                       "Category is required"        )][Outputs("'Category' is required.",                          "Bad Request"    )]
    public async Task Update_Toppings_Endpoint_Is_Called_With_Invalid_Or_Dangerous_Input()
    {
        await Runner.RunScenarioAsync(
            given => A_known_topping_exists(),
            and => Valid_update_topping_requests_with_an_invalid_field(TableFrom.Inputs<InvalidFieldFromRequest>()),
            when => The_invalid_update_topping_requests_are_submitted(),
            then => The_update_responses_should_each_contain_the_validation_error_for_the_invalid_field(VerifiableTableFrom.Outputs<VerifiableErrorResult>()));
    }

    #endregion
}
