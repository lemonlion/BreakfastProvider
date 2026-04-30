using BreakfastProvider.Api;
using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Tests.Component.Shared.Fakes.HttpFakes;
using BreakfastProvider.Tests.Component.Shared.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Infrastructure.Hosting;

public class TestHttpClientFactory(
    IHttpContextAccessor httpContextAccessor,
    FakeRequestStore fakeRequestStore,
    ComponentTestSettings settings) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var baseUrl = name switch
        {
            "CowService" => settings.CowServiceBaseUrl!,
            "GoatService" => settings.GoatServiceBaseUrl!,
            "SupplierService" => settings.SupplierServiceBaseUrl!,
            "KitchenService" => settings.KitchenServiceBaseUrl!,
            _ => "http://localhost:5239"
        };

        var label = name switch
        {
            "CowService" => "Cow Service",
            "GoatService" => "Goat Service",
            "SupplierService" => "Supplier Service",
            "KitchenService" => "Kitchen Service",
            _ => Documentation.ServiceNames.BreakfastProvider
        };

        var handler = new FakeHeaderPropagationHandler(httpContextAccessor)
        {
            InnerHandler = new CorrelationIdDelegatingHandler(httpContextAccessor)
            {
                InnerHandler = new RequestCapturingHandler(fakeRequestStore, httpContextAccessor, name)
                {
                    InnerHandler = new TestTrackingMessageHandler(
                        new TestTrackingMessageHandlerOptions
                        {
                            FixedNameForReceivingService = label,
                            CallerName = Documentation.ServiceNames.BreakfastProvider
                        },
                        httpContextAccessor)
                    {
                        InnerHandler = new HttpClientHandler()
                    }
                }
            }
        };

        return new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
    }
}
