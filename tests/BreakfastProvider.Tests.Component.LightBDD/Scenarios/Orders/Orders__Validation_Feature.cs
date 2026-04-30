using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.TabularAttributes;
using LightBDD.TabularAttributes.Attributes;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Input validation for order creation and status updates")]
public partial class Orders__Validation_Feature
{
    #region POST /orders Validation

    [Scenario]
    [HeadIn("Field",             "Value", "Reason"                              )][HeadOut("Error Message",                          "Response Status")]
    [Inputs("CustomerName",      "",      "Customer name is required"           )][Outputs("'Customer Name' is required.",            "Bad Request"    )]
    [Inputs("Items",             null,    "At least one item is required"       )][Outputs("The Items field is required.",            "Bad Request"    )]
    [Inputs("Items[0].ItemType", "",      "Item type is required"               )][Outputs("'Item Type' is required.",                "Bad Request"    )]
    [Inputs("Items[0].BatchId",  null,    "Batch ID is required"                )][Outputs("'Batch Id' is required.",                 "Bad Request"    )]
    [Inputs("Items[0].Quantity", 0,       "Quantity must be greater than zero"  )][Outputs("Quantity must be greater than zero.",     "Bad Request"    )]
    public async Task Orders_Endpoint_Is_Called_With_Invalid_Fields_Should_Return_A_Bad_Request_Response()
    {
        await Runner.RunScenarioAsync(
            given => Valid_order_requests_with_an_invalid_field(TableFrom.Inputs<InvalidFieldFromRequest>()),
            when => The_invalid_order_requests_are_submitted(),
            then => The_responses_should_each_contain_the_validation_error_for_the_invalid_field(VerifiableTableFrom.Outputs<VerifiableErrorResult>()));
    }

    #endregion

    #region PATCH /orders/{id}/status Validation

    [Scenario]
    [HeadIn("Field",  "Value", "Reason"              )][HeadOut("Error Message",          "Response Status")]
    [Inputs("Status", "",      "Status is required"   )][Outputs("'Status' is required.", "Bad Request"    )]
    public async Task Order_Status_Update_Endpoint_Is_Called_With_Invalid_Fields_Should_Return_A_Bad_Request_Response()
    {
        await Runner.RunScenarioAsync(
            given => Valid_status_update_requests_with_an_invalid_field(TableFrom.Inputs<InvalidFieldFromRequest>()),
            when => The_invalid_status_update_requests_are_submitted(),
            then => The_status_update_responses_should_each_contain_the_validation_error_for_the_invalid_field(VerifiableTableFrom.Outputs<VerifiableErrorResult>()));
    }

    #endregion
}
