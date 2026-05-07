using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Ingredients;

public class Ingredients_Goat_Milk_Feature_Flag_Tests : BaseFixture
{
    private GetGoatMilkSteps _goatMilkSteps = null!;

    public Ingredients_Goat_Milk_Feature_Flag_Tests() : base(delayAppCreation: true)
    {
    }

    [Fact]
    public void Goat_milk_endpoint_should_return_not_found_when_feature_is_disabled()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_goat_milk_feature_flag_is_disabled())
            .When(x => x.Goat_milk_is_requested())
            .Then(x => x.The_response_should_indicate_feature_disabled())
            .BDDfy();
    }

    [Fact]
    public void Goat_milk_endpoint_should_return_fresh_goat_milk_when_feature_is_enabled()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_goat_milk_feature_flag_is_enabled())
            .When(x => x.Goat_milk_is_requested())
            .Then(x => x.The_response_should_contain_fresh_goat_milk())
            .BDDfy();
    }

    #region Steps

    private void The_goat_milk_feature_flag_is_disabled()
    {
        CreateAppAndClient(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsGoatMilkEnabled)}"] = "false"
        });
        _goatMilkSteps = Get<GetGoatMilkSteps>();
    }

    private void The_goat_milk_feature_flag_is_enabled()
    {
        CreateAppAndClient(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsGoatMilkEnabled)}"] = "true"
        });
        _goatMilkSteps = Get<GetGoatMilkSteps>();
    }

    private async Task Goat_milk_is_requested()
    {
        await _goatMilkSteps.Retrieve();
    }

    private async Task The_response_should_indicate_feature_disabled()
    {
        _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await _goatMilkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        body.Should().Contain(DownstreamErrorMessages.FeatureDisabled);
    }

    private void The_response_should_contain_fresh_goat_milk()
    {
        _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _goatMilkSteps.GoatMilkResponse.GoatMilk.Should().Be(GoatServiceDefaults.FreshGoatMilk);
    }

    #endregion
}
