using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Toppings;

#pragma warning disable CS1998
public class Toppings_Update_Tests : BaseFixture
{
    private readonly PutToppingSteps _putSteps;

    private static readonly Guid KnownBlueberryToppingId = ToppingDefaults.KnownBlueberryToppingId;

    public Toppings_Update_Tests()
    {
        _putSteps = Get<PutToppingSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Updating_an_existing_topping_should_return_the_updated_topping()
    {
        // Given a known topping exists and a valid update request
        var toppingId = KnownBlueberryToppingId;
        _putSteps.Request = new TestUpdateToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };

        // When the topping is updated
        await _putSteps.Send(toppingId);

        // Then the update response should contain the updated topping
        Track.That(() => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _putSteps.ParseResponse();
        Track.That(() => _putSteps.Response!.ToppingId.Should().Be(KnownBlueberryToppingId));
        Track.That(() => _putSteps.Response!.Name.Should().Be(ToppingDefaults.Strawberries));
        Track.That(() => _putSteps.Response!.Category.Should().Be(ToppingDefaults.FruitCategory));
    }

    [Fact]
    public async Task Updating_a_non_existent_topping_should_return_not_found()
    {
        // Given a topping id that does not exist
        var toppingId = Guid.NewGuid();
        _putSteps.Request = new TestUpdateToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };

        // When the topping is updated
        await _putSteps.Send(toppingId);

        // Then the update response should indicate not found
        Track.That(() => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));
    }

    [Theory]
    [InlineData("Name", "<script>alert('xss')</script>", "Script tag in name", "Name contains potentially dangerous content.", "Bad Request")]
    [InlineData("Name", "<img src=x onerror=alert(1)>", "Event handler in name", "Name contains potentially dangerous content.", "Bad Request")]
    [InlineData("Category", "<script>alert('xss')</script>", "Script tag in category", "Category contains potentially dangerous content.", "Bad Request")]
    [InlineData("Category", "javascript:alert(1)", "Javascript protocol", "Category contains potentially dangerous content.", "Bad Request")]
    [InlineData("Name", "", "Name is required", "'Name' is required.", "Bad Request")]
    [InlineData("Category", "", "Category is required", "'Category' is required.", "Bad Request")]
    public async Task Update_topping_with_invalid_or_dangerous_input_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        // Given a known topping exists and an invalid update request
        var toppingId = KnownBlueberryToppingId;
        var validBase = new TestUpdateToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        // When the invalid update topping requests are submitted
        var responses = await ValidationHelper.SendPutValidationRequests(
            Client, RequestId, $"{Endpoints.Toppings}/{toppingId}", requests, new List<InvalidFieldFromRequest> { input });

        // Then the responses should contain the validation error
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        Track.That(() => actual.ErrorMessage.Should().Be(expectedError));
        Track.That(() => actual.ResponseStatus.Should().Be(expectedStatus));
    }
}
