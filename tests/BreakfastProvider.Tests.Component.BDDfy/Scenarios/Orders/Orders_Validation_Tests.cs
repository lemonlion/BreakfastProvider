using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Orders;

public class Orders_Validation_Tests : BaseFixture
{
    private InvalidFieldFromRequest _input = null!;
    private string _expectedError = null!;
    private string _expectedStatus = null!;
    private VerifiableErrorResult? _actual;

    [Theory]
    [InlineData("CustomerName", "", "Customer name is required", "'Customer Name' is required.", "Bad Request")]
    [InlineData("Items", null, "At least one item is required", "The Items field is required.", "Bad Request")]
    [InlineData("Items[0].ItemType", "", "Item type is required", "'Item Type' is required.", "Bad Request")]
    [InlineData("Items[0].BatchId", null, "Batch ID is required", "'Batch Id' is required.", "Bad Request")]
    [InlineData("Items[0].Quantity", "0", "Quantity must be greater than zero", "Quantity must be greater than zero.", "Bad Request")]
    public void Order_with_invalid_field_should_return_bad_request(
        string field, string? value, string reason, string expectedError, string expectedStatus)
    {
        _input = new InvalidFieldFromRequest(field, value, reason);
        _expectedError = expectedError;
        _expectedStatus = expectedStatus;

        this.Given(x => x.A_valid_order_request_with_an_invalid_field())
            .When(x => x.The_order_request_is_sent())
            .Then(x => x.The_response_should_contain_the_expected_validation_error())
            .BDDfy();
    }

    [Theory]
    [InlineData("Status", "", "Status is required", "'Status' is required.", "Bad Request")]
    public void Order_status_update_with_invalid_field_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        _input = new InvalidFieldFromRequest(field, value, reason);
        _expectedError = expectedError;
        _expectedStatus = expectedStatus;

        this.Given(x => x.A_valid_order_status_update_request_with_an_invalid_field())
            .When(x => x.The_order_status_update_request_is_sent())
            .Then(x => x.The_response_should_contain_the_expected_validation_error())
            .BDDfy();
    }

    #region Steps

    private Task A_valid_order_request_with_an_invalid_field()
    {
        return Task.CompletedTask;
    }

    private async Task The_order_request_is_sent()
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

        var requests = ValidationHelper.CreateValidationRequests(validBase, [_input]);
        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.Orders, requests, [_input]);
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        _actual = actualResults.Single();
    }

    private Task A_valid_order_status_update_request_with_an_invalid_field()
    {
        return Task.CompletedTask;
    }

    private async Task The_order_status_update_request_is_sent()
    {
        var validBase = new TestUpdateOrderStatusRequest
        {
            Status = OrderStatuses.Preparing
        };

        var requests = ValidationHelper.CreateValidationRequests(validBase, [_input]);
        var responses = await ValidationHelper.SendPatchValidationRequests(
            Client, RequestId, $"{Endpoints.Orders}/{Guid.NewGuid()}/status",
            requests, [_input]);
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        _actual = actualResults.Single();
    }

    private Task The_response_should_contain_the_expected_validation_error()
    {
        _actual!.ErrorMessage.Should().Be(_expectedError);
        _actual!.ResponseStatus.Should().Be(_expectedStatus);
        return Task.CompletedTask;
    }

    #endregion
}
