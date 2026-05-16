using Microsoft.Extensions.Configuration;

namespace BreakfastProvider.Tests.Component.Shared.Infrastructure.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static ComponentTestSettings GetComponentTestSettings(this IConfigurationBuilder builder)
    {
        return builder.GetComponentTestConfiguration().Get<ComponentTestSettings>()!;
    }

    public static IConfiguration GetComponentTestConfiguration(this IConfigurationBuilder builder)
    {
        var baseConfig = builder.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.componenttests.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var settings = baseConfig.Get<ComponentTestSettings>()!;

        // Only apply per-project port overrides in in-memory mode.
        // In Docker mode, fakes run in containers on the standard ports (5031-5035).
        if (settings.RunWithAnInMemoryCowService)
            builder.AddJsonFile("appsettings.componenttests.ports.json", optional: true, reloadOnChange: false);

        return builder.Build();
    }
}
