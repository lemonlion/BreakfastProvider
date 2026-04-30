using System.Net;
using System.Text.Json;
using BreakfastProvider.Api.Telemetry;
using Microsoft.Azure.Cosmos;

namespace BreakfastProvider.Api.Storage;

public class CosmosIdempotencyStore(
    ICosmosRepository<IdempotencyRecord> repository,
    ILogger<CosmosIdempotencyStore> logger) : IIdempotencyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<(bool Found, int StatusCode, T? Response)> TryGetAsync<T>(
        string key, CancellationToken cancellationToken = default) where T : class
    {
        var record = await repository.GetByIdAsync(key, key, cancellationToken);
        if (record is null)
        {
            DiagnosticsConfig.CacheMisses.Add(1, new KeyValuePair<string, object?>("cache.name", "idempotency"));
            return (false, 0, null);
        }

        DiagnosticsConfig.CacheHits.Add(1, new KeyValuePair<string, object?>("cache.name", "idempotency"));
        var response = JsonSerializer.Deserialize<T>(record.ResponsePayload, JsonOptions);
        return (true, record.ResponseStatusCode, response);
    }

    public async Task SetAsync<T>(
        string key, int statusCode, T response, int ttlSeconds,
        CancellationToken cancellationToken = default) where T : class
    {
        var record = new IdempotencyRecord
        {
            Id = key,
            PartitionKey = key,
            ResponsePayload = JsonSerializer.Serialize(response, JsonOptions),
            ResponseStatusCode = statusCode,
            CreatedAt = DateTime.UtcNow,
            Ttl = ttlSeconds
        };

        try
        {
            await repository.CreateAsync(record, key, cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            // Another request with the same key raced and stored first — expected behaviour
            logger.LogDebug("Idempotency record for key {Key} already exists (race condition handled)", key);
        }
    }
}
