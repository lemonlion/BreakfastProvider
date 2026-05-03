using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Common.Waffles;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using BreakfastProvider.Tests.Component.Shared.Models.Waffles;
using LightBDD.Framework;
using LightBDD.Framework.Parameters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Waffles;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Waffles__Creation_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostWafflesSteps _waffleSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Waffles__Creation_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _waffleSteps = Get<PostWafflesSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    private readonly List<TestWaffleRequest> _validationRequests = [];
    private readonly List<HttpResponseMessage> _validationResponses = [];
    private readonly List<InvalidFieldFromRequest> _validationInputs = [];

    private ToppingRulesConfig? _toppingRules;
    private ToppingRulesConfig ToppingRules => _toppingRules ??=
        AppFactory.Services.GetRequiredService<IOptions<ToppingRulesConfig>>().Value;
    private int MaxToppings => ToppingRules.MaxToppingsPerItem;

    #region Given

    private async Task<CompositeStep> A_valid_waffle_recipe_with_all_ingredients()
    {
        return Sub.Steps(
            _ => A_valid_request_body());
    }

    private async Task<CompositeStep> A_valid_request_body()
    {
        return Sub.Steps(
            _ => The_body_specifies_milk(),
            _ => The_body_specifies_eggs(),
            _ => The_body_specifies_flour(),
            _ => The_body_specifies_butter());
    }

    private async Task<CompositeStep> The_body_specifies_milk()
    {
        return Sub.Steps(
            _ => Milk_is_retrieved_from_the_get_milk_endpoint(),
            _ => The_milk_response_should_be_successful(),
            _ => Retrieved_milk_is_set_on_the_body());
    }

    private async Task Milk_is_retrieved_from_the_get_milk_endpoint()
        => await _milkSteps.Retrieve();

    private async Task The_milk_response_should_be_successful()
        => Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Retrieved_milk_is_set_on_the_body()
        => _waffleSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

    private async Task<CompositeStep> The_body_specifies_eggs()
    {
        return Sub.Steps(
            _ => Eggs_are_retrieved_from_the_get_eggs_endpoint(),
            _ => The_eggs_response_should_be_successful(),
            _ => Retrieved_eggs_are_set_on_the_body());
    }

    private async Task Eggs_are_retrieved_from_the_get_eggs_endpoint()
        => await _eggsSteps.Retrieve();

    private async Task The_eggs_response_should_be_successful()
        => Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Retrieved_eggs_are_set_on_the_body()
        => _waffleSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

    private async Task<CompositeStep> The_body_specifies_flour()
    {
        return Sub.Steps(
            _ => Flour_is_retrieved_from_the_get_flour_endpoint(),
            _ => The_flour_response_should_be_successful(),
            _ => Retrieved_flour_is_set_on_the_body());
    }

    private async Task Flour_is_retrieved_from_the_get_flour_endpoint()
        => await _flourSteps.Retrieve();

    private async Task The_flour_response_should_be_successful()
        => Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Retrieved_flour_is_set_on_the_body()
        => _waffleSteps.Request.Flour = _flourSteps.FlourResponse.Flour;

    private async Task The_body_specifies_butter()
        => _waffleSteps.Request.Butter = IngredientDefaults.UnsaltedButter;

    private async Task Valid_waffle_requests_with_an_invalid_field(InputTable<InvalidFieldFromRequest> inputs)
    {
        var validBase = new TestWaffleRequest
        {
            Milk = CowServiceDefaults.FreshMilk,
            Flour = IngredientDefaults.PlainFlour,
            Eggs = IngredientDefaults.FreeRangeEggs,
            Butter = IngredientDefaults.UnsaltedButter
        };

        _validationInputs.AddRange(inputs);
        _validationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, inputs));
    }

    private async Task The_max_toppings_per_item_is_LIMIT(int limit)
    {
        // Informational — config value is read from appsettings
    }

    private async Task The_request_has_more_toppings_than_the_configured_limit()
    {
        _waffleSteps.Request.Toppings = Enumerable.Range(0, MaxToppings + 1)
            .Select(i => $"Topping_{i}")
            .ToList();
    }

    #endregion

    #region When

    private async Task The_waffles_are_prepared()
        => await _waffleSteps.Send();

    private async Task The_invalid_waffle_requests_are_submitted()
        => _validationResponses.AddRange(
            await ValidationHelper.SendValidationRequests(Client, RequestId, Endpoints.Waffles, _validationRequests, _validationInputs));

    #endregion

    #region Then

    private async Task<CompositeStep> The_waffles_response_should_contain_a_valid_batch_with_all_ingredients()
    {
        return Sub.Steps(
            _ => The_response_http_status_should_be_created(),
            _ => The_response_should_be_valid_json(),
            _ => The_response_ingredients_should_include_milk(),
            _ => The_response_ingredients_should_include_eggs(),
            _ => The_response_ingredients_should_include_flour(),
            _ => The_response_ingredients_should_include_butter());
    }

    private async Task The_response_http_status_should_be_created()
        => Track.That(() => _waffleSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));

    private async Task The_response_should_be_valid_json()
        => await _waffleSteps.ParseResponse();

    private async Task The_response_ingredients_should_include_milk()
        => Track.That(() => _waffleSteps.Response!.Ingredients.Should().Contain(_milkSteps.MilkResponse.Milk));

    private async Task The_response_ingredients_should_include_eggs()
        => Track.That(() => _waffleSteps.Response!.Ingredients.Should().Contain(_eggsSteps.EggsResponse.Eggs));

    private async Task The_response_ingredients_should_include_flour()
        => Track.That(() => _waffleSteps.Response!.Ingredients.Should().Contain(_flourSteps.FlourResponse.Flour));

    private async Task The_response_ingredients_should_include_butter()
        => Track.That(() => _waffleSteps.Response!.Ingredients.Should().Contain(IngredientDefaults.UnsaltedButter));

    private async Task The_responses_should_each_contain_the_validation_error_for_the_invalid_field(
        VerifiableDataTable<VerifiableErrorResult> expectedOutputs)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_validationResponses);
        expectedOutputs.SetActual(actualResults);
    }

    private async Task<CompositeStep> The_waffles_response_should_indicate_too_many_toppings()
    {
        return Sub.Steps(
            _ => The_response_http_status_should_be_bad_request(),
            _ => The_response_should_contain_max_toppings_error());
    }

    private async Task The_response_http_status_should_be_bad_request()
        => Track.That(() => _waffleSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));

    private async Task The_response_should_contain_max_toppings_error()
    {
        var waffleErrorResponseBody = await _waffleSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => waffleErrorResponseBody.Should().Contain(WaffleValidationMessages.MaxToppingsExceeded));
    }

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_cow_service_should_have_received_a_milk_request()
        => _downstreamSteps.AssertCowServiceReceivedMilkRequest();

    #endregion
}
