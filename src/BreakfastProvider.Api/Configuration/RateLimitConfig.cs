namespace BreakfastProvider.Api.Configuration;

public class RateLimitConfig
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
}
