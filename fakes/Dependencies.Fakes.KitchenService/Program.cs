namespace Dependencies.Fakes.KitchenService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

        app.MapGet("/health-degraded", () => Results.StatusCode(503));

        app.MapPost("/prepare", (HttpContext context) =>
        {
            var scenario = context.Request.Headers["X-Fake-KitchenService-Scenario"].FirstOrDefault();

            return scenario switch
            {
                "KitchenBusy" => Results.Json(new { Status = "Busy", Message = "Kitchen is at capacity" }, statusCode: 503),
                _ => Results.Ok(new { Status = "Preparing", Message = "Order is being prepared" })
            };
        });

        app.MapGet("/status/{orderId:guid}", (Guid orderId) =>
        {
            return Results.Ok(new { OrderId = orderId, Status = "Preparing" });
        });

        app.Run();
    }
}
