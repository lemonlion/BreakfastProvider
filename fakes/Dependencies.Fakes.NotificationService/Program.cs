namespace Dependencies.Fakes.NotificationService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddGrpc();

        var app = builder.Build();

        app.MapGrpcService<FakeNotificationGrpcService>();
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

        await app.RunAsync();
    }
}
