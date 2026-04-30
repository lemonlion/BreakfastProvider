using BreakfastProvider.Api.Configuration;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.HttpClients;

public static class ServiceRegistration
{
    public static IServiceCollection AddDownstreamServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CowServiceConfig>(configuration.GetSection(nameof(CowServiceConfig)));
        services.Configure<GoatServiceConfig>(configuration.GetSection(nameof(GoatServiceConfig)));
        services.Configure<SupplierServiceConfig>(configuration.GetSection(nameof(SupplierServiceConfig)));
        services.Configure<KitchenServiceConfig>(configuration.GetSection(nameof(KitchenServiceConfig)));

        services.AddTransient<CorrelationIdDelegatingHandler>();

        services.AddHttpClient(HttpClientNames.CowService, (sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<CowServiceConfig>>().Value;
            client.BaseAddress = new Uri(config.BaseAddress);
        }).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

        services.AddHttpClient(HttpClientNames.GoatService, (sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<GoatServiceConfig>>().Value;
            client.BaseAddress = new Uri(config.BaseAddress);
        }).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

        services.AddHttpClient(HttpClientNames.SupplierService, (sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<SupplierServiceConfig>>().Value;
            client.BaseAddress = new Uri(config.BaseAddress);
        }).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

        services.AddHttpClient(HttpClientNames.KitchenService, (sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<KitchenServiceConfig>>().Value;
            client.BaseAddress = new Uri(config.BaseAddress);
        }).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

        return services;
    }
}
