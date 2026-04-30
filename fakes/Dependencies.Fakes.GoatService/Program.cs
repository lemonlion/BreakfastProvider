namespace Dependencies.Fakes.GoatService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

        app.MapGet("/goat-milk", (HttpContext context) =>
        {
            var scenario = context.Request.Headers["X-Fake-GoatService-Scenario"].FirstOrDefault();

            return scenario switch
            {
                "ServiceUnavailable" => Results.StatusCode(503),
                "InvalidResponse" => Results.Content("null", "application/json"),
                _ => Results.Ok(new { GoatMilk = "Some_Fresh_Goat_Milk" })
            };
        });

        app.Run();
    }
}
