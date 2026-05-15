using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

namespace BreakfastProvider.Tests.Component.Shared.Infrastructure.Hosting;

public static class InMemoryFakeHelper
{
    public static WebApplicationFactoryForSpecificUrl<TProgram> Create<TProgram>(
        string baseUrl,
        IConfiguration? config = null)
        where TProgram : class
    {
        EnsureCorrectWorkingDirectory();
        HttpFakesHelper.AssertPortIsNotInUse(baseUrl);
        var fixture = new WebApplicationFactoryForSpecificUrl<TProgram>(hostUrl: baseUrl, config);
        // Access Services to trigger host creation (starts Kestrel).
        // Do NOT call CreateDefaultClient() — the factory returns a dummy host
        // without a running TestServer, so CreateClient/CreateDefaultClient would throw.
        _ = fixture.Services;
        return fixture;
    }

    public static WebApplicationFactoryForSpecificUrl<TProgram> CreateForGrpc<TProgram>(
        string baseUrl,
        IConfiguration? config = null)
        where TProgram : class
    {
        EnsureCorrectWorkingDirectory();
        HttpFakesHelper.AssertPortIsNotInUse(baseUrl);
        var fixture = new WebApplicationFactoryForSpecificUrl<TProgram>(hostUrl: baseUrl, config, HttpProtocols.Http2);
        _ = fixture.Services;
        VerifyHttp2Support(baseUrl);
        return fixture;
    }

    public static void VerifyHttp2Support(string baseUrl)
    {
        using var handler = new SocketsHttpHandler();
        using var client = new HttpClient(handler);
        client.DefaultRequestVersion = new Version(2, 0);
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

        var response = client.GetAsync($"{baseUrl}/health").GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"gRPC fake at {baseUrl} failed HTTP/2 health check with status {response.StatusCode}");
        if (response.Version.Major < 2)
            throw new InvalidOperationException(
                $"gRPC fake at {baseUrl} responded with HTTP/{response.Version} instead of HTTP/2");
    }

    private static void EnsureCorrectWorkingDirectory()
    {
        // MTP (Microsoft.Testing.Platform) may launch test executables with a working directory
        // set to the repository root rather than the bin output directory. WebApplicationFactory
        // reads MvcTestingAppManifest.json from AppContext.BaseDirectory, but the fake service's
        // WebApplication.CreateBuilder() validates the content root using Directory.GetCurrentDirectory().
        // Ensure CWD matches the test output so content root resolution succeeds.
        var baseDir = AppContext.BaseDirectory;
        if (!string.Equals(Directory.GetCurrentDirectory(), baseDir, StringComparison.OrdinalIgnoreCase))
            Directory.SetCurrentDirectory(baseDir);
    }
}
