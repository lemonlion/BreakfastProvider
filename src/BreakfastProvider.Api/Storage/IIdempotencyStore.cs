namespace BreakfastProvider.Api.Storage;

public interface IIdempotencyStore
{
    Task<(bool Found, int StatusCode, T? Response)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, int statusCode, T response, int ttlSeconds, CancellationToken cancellationToken = default) where T : class;
}
