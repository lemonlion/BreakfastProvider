using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using LightBDD.Framework.Parameters;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998
public partial class Orders__Validation_Feature : BaseFixture
{
    private readonly List<TestOrderRequest> _orderValidationRequests = [];
    private readonly List<HttpResponseMessage> _orderValidationResponses = [];
    private readonly List<InvalidFieldFromRequest> _orderValidationInputs = [];

    private readonly List<TestUpdateOrderStatusRequest> _statusValidationRequests = [];
    private readonly List<HttpResponseMessage> _statusValidationResponses = [];
    private readonly List<InvalidFieldFromRequest> _statusValidationInputs = [];

    #region Given

    private async Task Valid_order_requests_with_an_invalid_field(InputTable<InvalidFieldFromRequest> inputs)
    {
        var validBase = new TestOrderRequest
        {
            CustomerName = "Test Customer",
            Items =
            [
                new TestOrderItemRequest
                {
                    ItemType = OrderDefaults.PancakeItemType,
                    BatchId = Guid.NewGuid(),
                    Quantity = 1
                }
            ],
            TableNumber = 1
        };

        _orderValidationInputs.AddRange(inputs);
        _orderValidationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, inputs));
    }

    private async Task Valid_status_update_requests_with_an_invalid_field(InputTable<InvalidFieldFromRequest> inputs)
    {
        var validBase = new TestUpdateOrderStatusRequest
        {
            Status = OrderStatuses.Preparing
        };

        _statusValidationInputs.AddRange(inputs);
        _statusValidationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, inputs));
    }

    #endregion

    #region When

    private async Task The_invalid_order_requests_are_submitted()
        => _orderValidationResponses.AddRange(
            await ValidationHelper.SendValidationRequests(Client, RequestId, Endpoints.Orders, _orderValidationRequests, _orderValidationInputs));

    private async Task The_invalid_status_update_requests_are_submitted()
        => _statusValidationResponses.AddRange(
            await ValidationHelper.SendPatchValidationRequests(Client, RequestId,
                $"{Endpoints.Orders}/{Guid.NewGuid()}/status", _statusValidationRequests, _statusValidationInputs));

    #endregion

    #region Then

    private async Task The_responses_should_each_contain_the_validation_error_for_the_invalid_field(
        VerifiableDataTable<VerifiableErrorResult> expectedOutputs)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_orderValidationResponses);
        expectedOutputs.SetActual(actualResults);
    }

    private async Task The_status_update_responses_should_each_contain_the_validation_error_for_the_invalid_field(
        VerifiableDataTable<VerifiableErrorResult> expectedOutputs)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_statusValidationResponses);
        expectedOutputs.SetActual(actualResults);
    }

    #endregion
}
