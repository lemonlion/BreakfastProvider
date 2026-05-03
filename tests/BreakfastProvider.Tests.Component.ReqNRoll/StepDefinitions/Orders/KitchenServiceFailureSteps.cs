using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class KitchenServiceFailureSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps)
{
    [Given("the kitchen service is configured to return busy")]
    public void GivenTheKitchenServiceIsConfiguredToReturnBusy()
    {
        orderSteps.AddHeader(FakeScenarioHeaders.KitchenService, FakeScenarios.KitchenBusy);
    }

    [Then("the order should still be created successfully despite the kitchen failure")]
    public async Task ThenTheOrderShouldStillBeCreatedSuccessfully()
    {
        Track.That(() => orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await orderSteps.ParseResponse();
        Track.That(() => orderSteps.Response!.OrderId.Should().NotBeEmpty());
    }
}
