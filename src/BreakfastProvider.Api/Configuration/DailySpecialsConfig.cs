namespace BreakfastProvider.Api.Configuration;

public class DailySpecialsConfig
{
    public int MaxOrdersPerSpecial { get; set; } = 10;
    public int IdempotencyTtlSeconds { get; set; } = 86400;
}
