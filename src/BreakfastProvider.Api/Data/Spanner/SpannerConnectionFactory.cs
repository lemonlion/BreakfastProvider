using Google.Cloud.Spanner.Data;

namespace BreakfastProvider.Api.Data.Spanner;

public interface ISpannerConnectionFactory
{
    SpannerConnection CreateConnection();
}

public class SpannerConnectionFactory(string connectionString) : ISpannerConnectionFactory
{
    public SpannerConnection CreateConnection() => new(connectionString);
}
