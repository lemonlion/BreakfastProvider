namespace Dependencies.Fakes.SupplierService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

        app.MapGet("/ingredients/{name}/availability", (string name, HttpContext context) =>
        {
            var scenario = context.Request.Headers["X-Fake-SupplierService-Scenario"].FirstOrDefault();

            return scenario switch
            {
                "OutOfStock" => Results.Ok(new { Name = name, IsAvailable = false, Reason = "Out of stock" }),
                "ServiceUnavailable" => Results.StatusCode(503),
                _ => Results.Ok(new { Name = name, IsAvailable = true, Reason = "In stock" })
            };
        });

        app.Run();
    }
}
