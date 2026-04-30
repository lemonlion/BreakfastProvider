namespace Dependencies.Fakes.CowService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

        app.MapGet("/milk", (HttpContext context) =>
        {
            var scenario = context.Request.Headers["X-Fake-CowService-Scenario"].FirstOrDefault();

            return scenario switch
            {
                "ServiceUnavailable" => Results.StatusCode(503),
                "Timeout" => Results.StatusCode(504),
                "InvalidResponse" => Results.Content("null", "application/json"),
                _ => Results.Ok(new { Milk = "Some_Fresh_Milk" })
            };
        });

        app.Run();
    }
}
