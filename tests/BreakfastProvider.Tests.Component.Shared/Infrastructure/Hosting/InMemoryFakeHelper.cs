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
        fixture.CreateDefaultClient();
        return fixture;
    }
}
