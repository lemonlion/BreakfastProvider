using Microsoft.Extensions.Configuration;

namespace BreakfastProvider.Tests.Component.LightBDD.Infrastructure.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static ComponentTestSettings GetComponentTestSettings(this IConfigurationBuilder builder)
    {
        return builder.GetComponentTestConfiguration().Get<ComponentTestSettings>()!;
    }

    public static IConfiguration GetComponentTestConfiguration(this IConfigurationBuilder builder)
    {
        return builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.componenttests.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }
}
