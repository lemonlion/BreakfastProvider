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

/// <summary>
/// Registered when Spanner is not configured (empty ProjectId).
/// Any call to CreateConnection will throw, preventing the Spanner SDK
/// from creating a connection with an invalid data source that crashes
/// the GC finalizer.
/// </summary>
public class NoOpSpannerConnectionFactory : ISpannerConnectionFactory
{
    public SpannerConnection CreateConnection() =>
        throw new InvalidOperationException("Spanner is not configured. Set SpannerConfig__ProjectId to use Spanner features.");
}
