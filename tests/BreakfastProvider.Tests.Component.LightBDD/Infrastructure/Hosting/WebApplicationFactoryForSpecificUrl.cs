using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BreakfastProvider.Tests.Component.LightBDD.Infrastructure.Hosting;

public class WebApplicationFactoryForSpecificUrl<TProgram>(string hostUrl, IConfiguration? config = null)
    : WebApplicationFactory<TProgram> where TProgram : class
{
    private IHost? _realHost;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (config != null)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddConfiguration(config);
            });
        }

        var dummyHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls(hostUrl);
        });

        _realHost = builder.Build();
        _realHost.Start();

        return dummyHost;
    }

    protected override void Dispose(bool disposing)
    {
        _realHost?.StopAsync().GetAwaiter().GetResult();
        _realHost?.Dispose();
        base.Dispose(disposing);
    }
}
