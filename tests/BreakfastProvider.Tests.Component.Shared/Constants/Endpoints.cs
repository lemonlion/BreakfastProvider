namespace BreakfastProvider.Tests.Component.Shared.Constants;

public static class Endpoints
{
    public const string Pancakes = "pancakes";
    public const string Waffles = "waffles";
    public const string Orders = "orders";
    public const string Milk = "milk";
    public const string Eggs = "eggs";
    public const string Flour = "flour";
    public const string GoatMilk = "goat-milk";
    public const string Toppings = "toppings";
    public const string Menu = "menu";
    public const string AuditLogs = "audit-logs";
    public const string Health = "health";
    public const string Heartbeat = "";
    public const string MenuCache = "menu/cache";
    public const string DailySpecials = "daily-specials";
    public const string DailySpecialsOrders = "daily-specials/orders";
    public const string GraphQL = "graphql";
    public const string Inventory = "inventory";
    public const string Staff = "staff";
    public const string Reservations = "reservations";
    public const string Feedback = "feedback";
    public const string CustomerPreferences = "customer-preferences";
    public const string EventGridWebhook = "webhooks/eventgrid";

    public static class Swagger
    {
        public const string SwaggerJson = "openapi/v1.json";
        public const string ScalarUI = "scalar/v1";

        public const string PancakesPath = "/" + Pancakes;
        public const string WafflesPath = "/" + Waffles;
        public const string OrdersPath = "/" + Orders;
        public const string OrderByIdPath = "/" + Orders + "/{orderId}";
        public const string ToppingsPath = "/" + Toppings;
        public const string MenuPath = "/" + Menu;
        public const string MilkPath = "/" + Milk;
        public const string EggsPath = "/" + Eggs;
        public const string FlourPath = "/" + Flour;
        public const string GoatMilkPath = "/" + GoatMilk;
        public const string AuditLogsPath = "/" + AuditLogs;
        public const string DailySpecialsPath = "/" + DailySpecials;
        public const string DailySpecialsOrdersPath = "/" + DailySpecialsOrders;
    }

    public static class AsyncApi
    {
        private const string BasePath = "/asyncapi";
        public const string AsyncApiSpec = $"{BasePath}/v1.json";
    }
}
