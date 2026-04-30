using System.Linq.Expressions;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace BreakfastProvider.Api.Storage;

public class CosmosRepository<T>(Container container) : ICosmosRepository<T> where T : class
{
    public async Task<T> CreateAsync(T item, string partitionKey, CancellationToken cancellationToken = default)
    {
        var response = await container.CreateItemAsync(item, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<T?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await container.ReadItemAsync<T>(id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using var iterator = container.GetItemLinqQueryable<T>()
            .Where(predicate)
            .ToOverridableFeedIterator();

        var results = new List<T>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, int, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using var iterator = container.GetItemLinqQueryable<T>()
            .Where(predicate)
            .ToOverridableFeedIterator();

        var results = new List<T>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task<(IReadOnlyList<T> Items, int TotalCount)> QueryPagedAsync(Expression<Func<T, bool>> predicate, int offset, int limit, CancellationToken cancellationToken = default)
    {
        // Count query — iterates matching items server-side to get the total.
        var allMatching = await QueryAsync(predicate, cancellationToken);
        var totalCount = allMatching.Count;

        // Apply offset/limit in-memory. For production scale, replace with
        // Cosmos SQL OFFSET/LIMIT via QueryDefinition for the page query.
        var items = allMatching
            .Skip(offset)
            .Take(limit)
            .ToList();

        return (items, totalCount);
    }

    public async Task<T> UpsertAsync(T item, string partitionKey, CancellationToken cancellationToken = default)
    {
        var response = await container.UpsertItemAsync(item, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }
}