using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Logging;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Infrastructure;

[Binding]
public class TelemetrySteps(
    AppManager appManager,
    PostOrderSteps orderSteps)
{
    private readonly InMemoryLoggerProvider _logProvider = new();

    [Given("the application is configured with an in-memory log capture")]
    public void GivenTheApplicationIsConfiguredWithAnInMemoryLogCapture()
    {
        appManager.CreateAppWithOverrides(additionalServices: services =>
        {
            services.AddSingleton<ILoggerFactory>(new LoggerFactory([_logProvider]));
        });
    }

    [Then("a structured log entry should have been captured for order creation")]
    public void ThenAStructuredLogEntryShouldHaveBeenCapturedForOrderCreation()
    {
        Track.That(() => _logProvider.Entries.Should().Contain(e => e.Message.Contains("created for customer")));
        Track.That(() => _logProvider.Entries.Should().Contain(e => e.Message.Contains(orderSteps.Request.CustomerName!)));
        Track.That(() => _logProvider.Entries.Should().Contain(e => e.Message.Contains("1 items")));
    }
}
