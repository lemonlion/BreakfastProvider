using Microsoft.Extensions.Configuration;

namespace BreakfastProvider.Tests.Component.Shared.Infrastructure.Hosting;

public static class InMemoryFakeHelper
{
    public static WebApplicationFactoryForSpecificUrl<TProgram> Create<TProgram>(
        string baseUrl,
        IConfiguration? config = null)
        where TProgram : class
    {
        HttpFakesHelper.AssertPortIsNotInUse(baseUrl);
        var fixture = new WebApplicationFactoryForSpecificUrl<TProgram>(hostUrl: baseUrl, config);
        // Access Services to trigger host creation (starts Kestrel).
        // Do NOT call CreateDefaultClient() — the factory returns a dummy host
        // without a running TestServer, so CreateClient/CreateDefaultClient would throw.
        _ = fixture.Services;
        return fixture;
    }
}
