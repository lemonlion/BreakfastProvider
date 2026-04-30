using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.TabularAttributes;
using LightBDD.TabularAttributes.Attributes;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Toppings;

[FeatureDescription($"/{Endpoints.Toppings} - XSS and input validation for toppings")]
public partial class Toppings__Xss_Validation_Feature
{
    #region Validation

    [Scenario]
    [HeadIn("Field",    "Value",                                  "Reason"                      )][HeadOut("Error Message",                                     "Response Status")]
    [Inputs("Name",     "<script>alert('xss')</script>",          "Script tag in name"          )][Outputs("Name contains potentially dangerous content.",      "Bad Request"    )]
    [Inputs("Name",     "<img src=x onerror=alert(1)>",           "Event handler in name"       )][Outputs("Name contains potentially dangerous content.",      "Bad Request"    )]
    [Inputs("Category", "<script>alert('xss')</script>",          "Script tag in category"      )][Outputs("Category contains potentially dangerous content.",  "Bad Request"    )]
    [Inputs("Category", "javascript:alert(1)",                    "Javascript protocol"         )][Outputs("Category contains potentially dangerous content.",  "Bad Request"    )]
    [Inputs("Name",     "",                                       "Name is required"            )][Outputs("'Name' is required.",                              "Bad Request"    )]
    [Inputs("Category", "",                                       "Category is required"        )][Outputs("'Category' is required.",                          "Bad Request"    )]
    public async Task Toppings_Endpoint_Is_Called_With_Invalid_Or_Dangerous_Input()
    {
        await Runner.RunScenarioAsync(
            given => Valid_topping_requests_with_an_invalid_field(TableFrom.Inputs<InvalidFieldFromRequest>()),
            when => The_invalid_topping_requests_are_submitted(),
            then => The_responses_should_each_contain_the_validation_error_for_the_invalid_field(VerifiableTableFrom.Outputs<VerifiableErrorResult>()));
    }

    #endregion
}
