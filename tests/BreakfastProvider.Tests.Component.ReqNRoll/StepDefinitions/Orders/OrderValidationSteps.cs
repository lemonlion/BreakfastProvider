using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class OrderValidationSteps(AppManager appManager)
{
    private readonly List<TestOrderRequest> _orderValidationRequests = [];
    private readonly List<HttpResponseMessage> _orderValidationResponses = [];
    private readonly List<InvalidFieldFromRequest> _orderValidationInputs = [];
    private readonly List<TestUpdateOrderStatusRequest> _statusValidationRequests = [];
    private readonly List<HttpResponseMessage> _statusValidationResponses = [];
    private readonly List<InvalidFieldFromRequest> _statusValidationInputs = [];

    [Given(@"a valid order request with ""(.*)"" set to ""(.*)""")]
    public void GivenAValidOrderRequestWithFieldSetToValue(string field, string value)
    {
        var validBase = new TestOrderRequest
        {
            CustomerName = $"TestCustomer_{Random.Shared.NextInt64()}",
            TableNumber = 1,
            Items = [new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = Guid.NewGuid(), Quantity = 1 }]
        };

        var input = new InvalidFieldFromRequest(field, value, string.Empty);
        _orderValidationInputs.Add(input);
        _orderValidationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, [input]));
    }

    [Given(@"a valid status update request with ""(.*)"" set to ""(.*)""")]
    public void GivenAValidStatusUpdateRequestWithFieldSetToValue(string field, string value)
    {
        var validBase = new TestUpdateOrderStatusRequest { Status = OrderStatuses.Preparing };

        var input = new InvalidFieldFromRequest(field, value, string.Empty);
        _statusValidationInputs.Add(input);
        _statusValidationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, [input]));
    }

    [When("the invalid order request is submitted")]
    public async Task WhenTheInvalidOrderRequestIsSubmitted()
    {
        _orderValidationResponses.AddRange(
            await ValidationHelper.SendValidationRequests(
                appManager.Client, appManager.RequestId, Endpoints.Orders, _orderValidationRequests, _orderValidationInputs));
    }

    [When("the invalid status update request is submitted")]
    public async Task WhenTheInvalidStatusUpdateRequestIsSubmitted()
    {
        _statusValidationResponses.AddRange(
            await ValidationHelper.SendPatchValidationRequests(
                appManager.Client, appManager.RequestId, $"{Endpoints.Orders}/{Guid.NewGuid()}/status", _statusValidationRequests, _statusValidationInputs));
    }

    [Then(@"the order response should contain error ""(.*)"" with status ""(.*)""")]
    public async Task ThenTheOrderResponseShouldContainError(string errorMessage, string responseStatus)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_orderValidationResponses);
        actualResults.Should().Contain(r => r.ErrorMessage.Contains(errorMessage));
    }

    [Then(@"the status update response should contain error ""(.*)"" with status ""(.*)""")]
    public async Task ThenTheStatusUpdateResponseShouldContainError(string errorMessage, string responseStatus)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_statusValidationResponses);
        actualResults.Should().Contain(r => r.ErrorMessage.Contains(errorMessage));
    }
}
