using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Toppings;

public class Toppings_Update_Tests : BaseFixture
{
    private readonly PutToppingSteps _putSteps;

    private static readonly Guid KnownBlueberryToppingId = ToppingDefaults.KnownBlueberryToppingId;

    private Guid _toppingId;

    public Toppings_Update_Tests()
    {
        _putSteps = Get<PutToppingSteps>();
    }

    [Fact]
    [HappyPath]
    public void Updating_an_existing_topping_should_return_the_updated_topping()
    {
        this.Given(x => x.A_known_topping_exists_and_a_valid_update_request())
            .When(x => x.The_topping_is_updated())
            .Then(x => x.The_update_response_should_contain_the_updated_topping())
            .BDDfy();
    }

    [Fact]
    public void Updating_a_non_existent_topping_should_return_not_found()
    {
        this.Given(x => x.A_topping_id_that_does_not_exist_and_a_valid_update_request())
            .When(x => x.The_topping_is_updated())
            .Then(x => x.The_update_response_should_indicate_not_found())
            .BDDfy();
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
        var toppingId = KnownBlueberryToppingId;
        var validBase = new TestUpdateToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        var responses = await ValidationHelper.SendPutValidationRequests(
            Client, RequestId, $"{Endpoints.Toppings}/{toppingId}", requests, new List<InvalidFieldFromRequest> { input });

        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        actual.ErrorMessage.Should().Be(expectedError);
        actual.ResponseStatus.Should().Be(expectedStatus);
        this.BDDfy();
    }

    #region Steps

    private void A_known_topping_exists_and_a_valid_update_request()
    {
        _toppingId = KnownBlueberryToppingId;
        _putSteps.Request = new TestUpdateToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };
    }

    private void A_topping_id_that_does_not_exist_and_a_valid_update_request()
    {
        _toppingId = Guid.NewGuid();
        _putSteps.Request = new TestUpdateToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };
    }

    private async Task The_topping_is_updated()
    {
        await _putSteps.Send(_toppingId);
    }

    private async Task The_update_response_should_contain_the_updated_topping()
    {
        _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _putSteps.ParseResponse();
        _putSteps.Response!.ToppingId.Should().Be(KnownBlueberryToppingId);
        _putSteps.Response!.Name.Should().Be(ToppingDefaults.Strawberries);
        _putSteps.Response!.Category.Should().Be(ToppingDefaults.FruitCategory);
    }

    private void The_update_response_should_indicate_not_found()
    {
        _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
