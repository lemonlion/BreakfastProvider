using BreakfastProvider.Tests.Component.Shared.Common;
using BreakfastProvider.Tests.Component.Shared.Common.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerPreferences;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Feedback;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Inventory;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Muffins;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Common.Reservations;
using BreakfastProvider.Tests.Component.Shared.Common.Staff;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Common.Waffles;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace BreakfastProvider.Tests.Component.ReqNRoll.Support;

public class DependencyInjectionSetup
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton(AppManager.Settings);
        services.AddSingleton(AppManager.FakeRequestStore);
        services.AddSingleton(AppManager.ConsumedKafkaMessageStore);

        services.AddScoped<AppManager>();

        services.AddScoped(sp =>
        {
            var appManager = sp.GetRequiredService<AppManager>();
            return new RequestContext(() => appManager.Client, appManager.RequestId);
        });

        services.AddScoped<GetMilkSteps>();
        services.AddScoped<GetEggsSteps>();
        services.AddScoped<GetFlourSteps>();
        services.AddScoped<GetGoatMilkSteps>();
        services.AddScoped<PostPancakesSteps>();
        services.AddScoped<PostWafflesSteps>();
        services.AddScoped<PostMuffinsSteps>();
        services.AddScoped<PostOrderSteps>();
        services.AddScoped<GetOrderSteps>();
        services.AddScoped<ListOrdersSteps>();
        services.AddScoped<PostToppingsSteps>();
        services.AddScoped<GetToppingsSteps>();
        services.AddScoped<GetMenuSteps>();
        services.AddScoped<GetAuditLogsSteps>();
        services.AddScoped<DownstreamRequestSteps>();
        services.AddScoped<OutboxSteps>();
        services.AddScoped<PatchOrderStatusSteps>();
        services.AddScoped<DeleteToppingSteps>();
        services.AddScoped<PutToppingSteps>();
        services.AddScoped<GetDailySpecialsSteps>();
        services.AddScoped<PostDailySpecialOrderSteps>();
        services.AddScoped<ResetDailySpecialOrdersSteps>();
        services.AddScoped<GraphQlReportingSteps>();
        services.AddScoped<PostInventorySteps>();
        services.AddScoped<GetInventorySteps>();
        services.AddScoped<PutInventorySteps>();
        services.AddScoped<DeleteInventorySteps>();
        services.AddScoped<PostStaffSteps>();
        services.AddScoped<GetStaffSteps>();
        services.AddScoped<PostReservationSteps>();
        services.AddScoped<GetReservationSteps>();
        services.AddScoped<CancelReservationSteps>();
        services.AddScoped<PostFeedbackSteps>();
        services.AddScoped<GetFeedbackSteps>();
        services.AddScoped<PutCustomerPreferenceSteps>();
        services.AddScoped<GetCustomerPreferenceSteps>();

        return services;
    }
}