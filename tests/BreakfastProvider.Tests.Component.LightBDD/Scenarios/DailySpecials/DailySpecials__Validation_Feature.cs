using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.TabularAttributes;
using LightBDD.TabularAttributes.Attributes;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.DailySpecials;

[FeatureDescription($"/{Endpoints.DailySpecialsOrders} - Input validation for daily special orders")]
public partial class DailySpecials__Validation_Feature
{
    [Scenario]
    [HeadIn("Field",     "Value", "Reason"                             )][HeadOut("Error Message",                             "Response Status")]
    [Inputs("SpecialId", null,    "Special ID is required"             )][Outputs("'Special Id' is required.",                "Bad Request"    )]
    [Inputs("Quantity",  0,       "Quantity must be greater than zero" )][Outputs("Quantity must be greater than zero.",       "Bad Request"    )]
    public async Task Daily_Special_Order_Endpoint_Is_Called_With_Invalid_Fields_Should_Return_A_Bad_Request_Response()
    {
        await Runner.RunScenarioAsync(
            given => Valid_daily_special_order_requests_with_an_invalid_field(TableFrom.Inputs<InvalidFieldFromRequest>()),
            when => The_invalid_daily_special_order_requests_are_submitted(),
            then => The_responses_should_each_contain_the_validation_error_for_the_invalid_field(VerifiableTableFrom.Outputs<VerifiableErrorResult>()));
    }
}
