using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Orders;

public class Orders_Validation_Tests : BaseFixture
{
    #region POST /orders Validation

    [Theory]
    [InlineData("CustomerName", "", "Customer name is required", "'Customer Name' is required.", "Bad Request")]
    [InlineData("Items", null, "At least one item is required", "The Items field is required.", "Bad Request")]
    [InlineData("Items[0].ItemType", "", "Item type is required", "'Item Type' is required.", "Bad Request")]
    [InlineData("Items[0].BatchId", null, "Batch ID is required", "'Batch Id' is required.", "Bad Request")]
    [InlineData("Items[0].Quantity", "0", "Quantity must be greater than zero", "Quantity must be greater than zero.", "Bad Request")]
    public async Task Order_with_invalid_field_should_return_bad_request(
        string field, string? value, string reason, string expectedError, string expectedStatus)
    {
        // Given valid order requests with an invalid field
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

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        // When the invalid order requests are submitted
        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.Orders, requests, new List<InvalidFieldFromRequest> { input });

        // Then the responses should contain the validation error
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        Track.That(() => actual.ErrorMessage.Should().Be(expectedError));
        Track.That(() => actual.ResponseStatus.Should().Be(expectedStatus));
    }

    #endregion

    #region PATCH /orders/{id}/status Validation

    [Theory]
    [InlineData("Status", "", "Status is required", "'Status' is required.", "Bad Request")]
    public async Task Order_status_update_with_invalid_field_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        // Given valid status update requests with an invalid field
        var validBase = new TestUpdateOrderStatusRequest
        {
            Status = OrderStatuses.Preparing
        };

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        // When the invalid status update requests are submitted
        var responses = await ValidationHelper.SendPatchValidationRequests(
            Client, RequestId, $"{Endpoints.Orders}/{Guid.NewGuid()}/status",
            requests, new List<InvalidFieldFromRequest> { input });

        // Then the responses should contain the validation error
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        Track.That(() => actual.ErrorMessage.Should().Be(expectedError));
        Track.That(() => actual.ResponseStatus.Should().Be(expectedStatus));
    }

    #endregion
}
