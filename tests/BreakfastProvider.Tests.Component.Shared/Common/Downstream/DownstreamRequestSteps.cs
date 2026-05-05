using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Fakes.HttpFakes;

namespace BreakfastProvider.Tests.Component.Shared.Common.Downstream;

public class DownstreamRequestSteps(FakeRequestStore fakeRequestStore, RequestContext context)
{
    public void AssertCowServiceReceivedMilkRequest()
    {
        var requests = fakeRequestStore.GetRequests(context.RequestId, ServiceNames.CowService);
        requests.Should().Contain(r => r.RequestUri!.AbsolutePath == "/milk"
            && r.Method == HttpMethod.Get);
    }

    public void AssertGoatServiceReceivedGoatMilkRequest()
    {
        var requests = fakeRequestStore.GetRequests(context.RequestId, ServiceNames.GoatService);
        requests.Should().Contain(r => r.RequestUri!.AbsolutePath == "/goat-milk"
            && r.Method == HttpMethod.Get);
    }

    public void AssertSupplierServiceReceivedAvailabilityRequest()
    {
        var requests = fakeRequestStore.GetRequests(context.RequestId, ServiceNames.SupplierService);
        requests.Should().Contain(r => r.RequestUri!.AbsolutePath.Contains("/availability")
            && r.Method == HttpMethod.Get);
    }

    public void AssertKitchenServiceReceivedPreparationRequest()
    {
        var requests = fakeRequestStore.GetRequests(context.RequestId, ServiceNames.KitchenService);
        requests.Should().Contain(r => r.RequestUri!.AbsolutePath == "/prepare"
            && r.Method == HttpMethod.Post);
    }

    public void AssertDownstreamReceivedCorrelationId(string serviceName, string expectedCorrelationId)
    {
        var requests = fakeRequestStore.GetRequests(context.RequestId, serviceName);
        requests.Should().NotBeEmpty();
        var request = requests.First();
        request.Headers.Should().ContainKey(CustomHeaders.CorrelationId);
        request.Headers[CustomHeaders.CorrelationId].Should().Be(expectedCorrelationId);
    }
}
