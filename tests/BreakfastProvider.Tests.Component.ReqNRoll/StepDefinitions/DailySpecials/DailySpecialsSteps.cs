using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.DailySpecials;

[Binding]
public class DailySpecialsSteps(
    AppManager appManager,
    GetDailySpecialsSteps getSteps,
    PostDailySpecialOrderSteps postSteps,
    ResetDailySpecialOrdersSteps resetSteps)
{
    private DailySpecialsConfig? _dailySpecialsConfig;
    private int MaxOrdersPerSpecial => (_dailySpecialsConfig ??=
        appManager.AppFactory.Services.GetRequiredService<IOptions<DailySpecialsConfig>>().Value).MaxOrdersPerSpecial;

    private string _idempotencyKey = null!;
    private Guid _firstConfirmationId;
    private Guid _secondConfirmationId;

    private readonly List<TestDailySpecialOrderRequest> _validationRequests = [];
    private readonly List<HttpResponseMessage> _validationResponses = [];
    private readonly List<InvalidFieldFromRequest> _validationInputs = [];

    // --- Ordering ---
    [Given("the cinnamon swirl order count is reset")]
    public async Task GivenTheCinnamonSwirlOrderCountIsReset()
        => await resetSteps.Reset(DailySpecialDefaults.CinnamonSwirlId);

    [Given("the matcha waffles order count is reset")]
    public async Task GivenTheMatchaWafflesOrderCountIsReset()
        => await resetSteps.Reset(DailySpecialDefaults.MatchaWafflesId);

    [Given("the lemon ricotta order count is reset")]
    public async Task GivenTheLemonRicottaOrderCountIsReset()
        => await resetSteps.Reset(DailySpecialDefaults.LemonRicottaId);

    [Given("a valid daily special order request for cinnamon swirl")]
    public void GivenAValidDailySpecialOrderRequestForCinnamonSwirl()
    {
        postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };
    }

    [Given("the matcha waffles special has been ordered up to the configured limit")]
    public async Task GivenTheMatchaWafflesSpecialHasBeenOrderedUpToTheConfiguredLimit()
    {
        postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.MatchaWafflesId,
            Quantity = MaxOrdersPerSpecial
        };
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Given("a daily special order for lemon ricotta of quantity one is placed")]
    public async Task GivenADailySpecialOrderForLemonRicottaOfQuantityOneIsPlaced()
    {
        postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.LemonRicottaId,
            Quantity = 1
        };
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [When("the daily special order is submitted")]
    public async Task WhenTheDailySpecialOrderIsSubmitted() => await postSteps.Send();

    [When("another order is placed for the matcha waffles special")]
    public async Task WhenAnotherOrderIsPlacedForTheMatchaWafflesSpecial()
    {
        postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.MatchaWafflesId,
            Quantity = 1
        };
        await postSteps.Send();
    }

    [When("the available daily specials are requested")]
    public async Task WhenTheAvailableDailySpecialsAreRequested() => await getSteps.Retrieve();

    [Then("the daily special order response should contain a valid confirmation")]
    public async Task ThenTheDailySpecialOrderResponseShouldContainAValidConfirmation()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        postSteps.Response!.SpecialId.Should().Be(DailySpecialDefaults.CinnamonSwirlId);
        postSteps.Response!.OrderConfirmationId.Should().NotBeEmpty();
    }

    [Then("the daily specials response should contain all expected specials")]
    public async Task ThenTheDailySpecialsResponseShouldContainAllExpectedSpecials()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseResponse();
        getSteps.Response.Should().HaveCount(DailySpecialDefaults.ExpectedSpecialsCount);
    }

    [Then("the response should indicate the daily special is sold out")]
    public void ThenTheResponseShouldIndicateTheDailySpecialIsSoldOut()
        => postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict);

    [Then("the lemon ricotta special should have one fewer remaining")]
    public async Task ThenTheLemonRicottaSpecialShouldHaveOneFewerRemaining()
    {
        await getSteps.ParseResponse();
        var lemonRicotta = getSteps.Response!.Single(s => s.SpecialId == DailySpecialDefaults.LemonRicottaId);
        lemonRicotta.RemainingQuantity.Should().Be(MaxOrdersPerSpecial - 1);
    }

    // --- Idempotency ---
    [Given("an order request with an idempotency key")]
    public void GivenAnOrderRequestWithAnIdempotencyKey()
    {
        _idempotencyKey = Guid.NewGuid().ToString();
        postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };
    }

    [Given("an order request for the same special")]
    public void GivenAnOrderRequestForTheSameSpecial()
    {
        postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };
    }

    [When("the order is submitted twice with the same idempotency key")]
    public async Task WhenTheOrderIsSubmittedTwiceWithTheSameIdempotencyKey()
    {
        postSteps.AddHeader(CustomHeaders.IdempotencyKey, _idempotencyKey);

        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _firstConfirmationId = postSteps.Response!.OrderConfirmationId;

        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _secondConfirmationId = postSteps.Response!.OrderConfirmationId;
    }

    [When("the order is submitted with two different idempotency keys")]
    public async Task WhenTheOrderIsSubmittedWithTwoDifferentIdempotencyKeys()
    {
        postSteps.AddHeader(CustomHeaders.IdempotencyKey, Guid.NewGuid().ToString());
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _firstConfirmationId = postSteps.Response!.OrderConfirmationId;

        postSteps.AddHeader(CustomHeaders.IdempotencyKey, Guid.NewGuid().ToString());
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _secondConfirmationId = postSteps.Response!.OrderConfirmationId;
    }

    [Then("both responses should return the same confirmation id")]
    public void ThenBothResponsesShouldReturnTheSameConfirmationId()
        => _firstConfirmationId.Should().Be(_secondConfirmationId);

    [Then("the responses should have different confirmation ids")]
    public void ThenTheResponsesShouldHaveDifferentConfirmationIds()
        => _firstConfirmationId.Should().NotBe(_secondConfirmationId);

    // --- Validation ---
    [Given(@"a valid daily special order request with ""(.*)"" set to ""(.*)""")]
    public void GivenAValidDailySpecialOrderRequestWithFieldSetToValue(string field, string value)
    {
        var validBase = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };
        var input = new InvalidFieldFromRequest(field, value, string.Empty);
        _validationInputs.Add(input);
        _validationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, [input]));
    }

    [When("the invalid daily special order request is submitted")]
    public async Task WhenTheInvalidDailySpecialOrderRequestIsSubmitted()
    {
        _validationResponses.AddRange(
            await ValidationHelper.SendValidationRequests(
                appManager.Client, appManager.RequestId, Endpoints.DailySpecialsOrders, _validationRequests, _validationInputs));
    }

    [Then(@"the daily special response should contain error ""(.*)"" with status ""(.*)""")]
    public async Task ThenTheDailySpecialResponseShouldContainErrorWithStatus(string errorMessage, string responseStatus)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_validationResponses);
        actualResults.Should().Contain(r => r.ErrorMessage.Contains(errorMessage));
    }

    // --- Not Found ---
    [Given("a daily special order request for a non-existent special")]
    public void GivenADailySpecialOrderRequestForANonExistentSpecial()
    {
        postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = Guid.NewGuid(),
            Quantity = 1
        };
    }

    [Then("the daily special response should indicate not found")]
    public void ThenTheDailySpecialResponseShouldIndicateNotFound()
        => postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
