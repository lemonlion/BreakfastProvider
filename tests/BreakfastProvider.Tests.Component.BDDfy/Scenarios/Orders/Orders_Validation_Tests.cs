using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Orders;

public class Orders_Validation_Tests : BaseFixture
{
    [Theory]
    [InlineData("CustomerName", "", "Customer name is required", "'Customer Name' is required.", "Bad Request")]
    [InlineData("Items", null, "At least one item is required", "The Items field is required.", "Bad Request")]
    [InlineData("Items[0].ItemType", "", "Item type is required", "'Item Type' is required.", "Bad Request")]
    [InlineData("Items[0].BatchId", null, "Batch ID is required", "'Batch Id' is required.", "Bad Request")]
    [InlineData("Items[0].Quantity", "0", "Quantity must be greater than zero", "Quantity must be greater than zero.", "Bad Request")]
    public async Task Order_with_invalid_field_should_return_bad_request(
        string field, string? value, string reason, string expectedError, string expectedStatus)
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

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.Orders, requests, new List<InvalidFieldFromRequest> { input });

        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        actual.ErrorMessage.Should().Be(expectedError);
        actual.ResponseStatus.Should().Be(expectedStatus);
        this.BDDfy();
    }

    [Theory]
    [InlineData("Status", "", "Status is required", "'Status' is required.", "Bad Request")]
    public async Task Order_status_update_with_invalid_field_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        var validBase = new TestUpdateOrderStatusRequest
        {
            Status = OrderStatuses.Preparing
        };

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        var responses = await ValidationHelper.SendPatchValidationRequests(
            Client, RequestId, $"{Endpoints.Orders}/{Guid.NewGuid()}/status",
            requests, new List<InvalidFieldFromRequest> { input });

        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        actual.ErrorMessage.Should().Be(expectedError);
        actual.ResponseStatus.Should().Be(expectedStatus);
        this.BDDfy();
    }
}
