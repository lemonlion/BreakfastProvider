using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using NSubstitute;

namespace BreakfastProvider.Tests.Component.LightBDD.Fakes.Cosmos;

/// <summary>
/// In-memory replacement for <see cref="Container"/> that stores items as JSON.
/// Supports multiple document types in a single container (matching the production
/// layout where orders, recipes, and audit logs share one Cosmos container).
/// Adapted from an existing in-memory Cosmos container implementation.
/// </summary>
public class InMemoryContainer : Container
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ConcurrentDictionary<string, string> _items = new();

    private static Task<ItemResponse<T>> CreateResponse<T>(T item, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = Substitute.For<ItemResponse<T>>();
        response.Resource.Returns(item);
        response.StatusCode.Returns(statusCode);
        return Task.FromResult(response);
    }

    public override Task<ItemResponse<T>> CreateItemAsync<T>(
        T item,
        PartitionKey? partitionKey = null!,
        ItemRequestOptions requestOptions = null!,
        CancellationToken cancellationToken = default)
    {
        var id = GetId(item);
        var json = JsonSerializer.Serialize(item, JsonOptions);

        if (!_items.TryAdd(id, json))
            throw new CosmosException("Item already exists.", HttpStatusCode.Conflict, (int)HttpStatusCode.Conflict, "409", 0);

        return CreateResponse(item, HttpStatusCode.Created);
    }

    public override Task<ItemResponse<T>> ReadItemAsync<T>(
        string id,
        PartitionKey partitionKey,
        ItemRequestOptions requestOptions = null!,
        CancellationToken cancellationToken = default)
    {
        if (_items.TryGetValue(id, out var json))
        {
            var item = JsonSerializer.Deserialize<T>(json, JsonOptions)!;
            return CreateResponse(item);
        }

        throw new CosmosException("Resource not found.", HttpStatusCode.NotFound, (int)HttpStatusCode.NotFound, "404", 0);
    }

    public override Task<ItemResponse<T>> UpsertItemAsync<T>(
        T item,
        PartitionKey? partitionKey = null!,
        ItemRequestOptions requestOptions = null!,
        CancellationToken cancellationToken = default)
    {
        var id = GetId(item);
        var json = JsonSerializer.Serialize(item, JsonOptions);
        var isNew = _items.TryAdd(id, json);
        if (!isNew)
            _items[id] = json;
        return CreateResponse(item, isNew ? HttpStatusCode.Created : HttpStatusCode.OK);
    }

    public override Task<ItemResponse<T>> ReplaceItemAsync<T>(
        T item,
        string id,
        PartitionKey? partitionKey = null!,
        ItemRequestOptions requestOptions = null!,
        CancellationToken cancellationToken = default)
    {
        if (!_items.ContainsKey(id))
            throw new CosmosException("Resource not found.", HttpStatusCode.NotFound, (int)HttpStatusCode.NotFound, "404", 0);

        var json = JsonSerializer.Serialize(item, JsonOptions);
        _items[id] = json;
        return CreateResponse(item);
    }

    public override Task<ItemResponse<T>> DeleteItemAsync<T>(
        string id,
        PartitionKey partitionKey,
        ItemRequestOptions requestOptions = null!,
        CancellationToken cancellationToken = default)
    {
        if (!_items.TryRemove(id, out _))
            throw new CosmosException("Resource not found.", HttpStatusCode.NotFound, (int)HttpStatusCode.NotFound, "404", 0);

        return CreateResponse(default(T)!, HttpStatusCode.NoContent);
    }

    public override IOrderedQueryable<T> GetItemLinqQueryable<T>(bool allowSynchronousQueryExecution = false, string continuationToken = null!, QueryRequestOptions requestOptions = null!, CosmosLinqSerializerOptions linqSerializerOptions = null!)
    {
        var items = _items.Values
            .Select(json =>
            {
                try { return JsonSerializer.Deserialize<T>(json, JsonOptions); }
                catch { return default; }
            })
            .Where(item => item is not null)
            .Cast<T>();

        return items.AsQueryable().OrderBy(x => Guid.NewGuid()); // If multiple partitions, there is no guarantee of ordering
    }

    public override Task<ContainerResponse> ReadContainerAsync(
        ContainerRequestOptions requestOptions = null!,
        CancellationToken cancellationToken = default)
    {
        var response = Substitute.For<ContainerResponse>();
        response.StatusCode.Returns(HttpStatusCode.OK);
        return Task.FromResult(response);
    }

    public override Task<FeedResponse<T>> ReadManyItemsAsync<T>(IReadOnlyList<(string id, PartitionKey partitionKey)> items, ReadManyRequestOptions readManyRequestOptions = null!, CancellationToken cancellationToken = default)
    {
        var results = items
            .Where(i => _items.ContainsKey(i.id))
            .Select(i => JsonSerializer.Deserialize<T>(_items[i.id], JsonOptions)!)
            .ToList();

        var response = Substitute.For<FeedResponse<T>>();
        response.Resource.Returns(results);
        response.StatusCode.Returns(HttpStatusCode.OK);
        response.Count.Returns(results.Count);
        response.GetEnumerator().Returns(results.GetEnumerator());
        return Task.FromResult(response);
    }

    private static string GetId<T>(T item)
    {
        var idProp = typeof(T).GetProperty("Id");
        return idProp?.GetValue(item)?.ToString() ?? Guid.NewGuid().ToString();
    }

    #region unused override methods

    public override string Id => "in-memory";
    public override Database Database => throw new NotImplementedException();
    public override Conflicts Conflicts => throw new NotImplementedException();
    public override Scripts Scripts => throw new NotImplementedException();

    public override Task<ResponseMessage> CreateItemStreamAsync(Stream streamPayload, PartitionKey partitionKey, ItemRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override TransactionalBatch CreateTransactionalBatch(PartitionKey partitionKey) => new InMemoryTransactionalBatch(_items, JsonOptions);
    public override Task<ContainerResponse> DeleteContainerAsync(ContainerRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ResponseMessage> DeleteContainerStreamAsync(ContainerRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ResponseMessage> DeleteItemStreamAsync(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override ChangeFeedEstimator GetChangeFeedEstimator(string processorName, Container leaseContainer) => throw new NotImplementedException();
    public override ChangeFeedProcessorBuilder GetChangeFeedEstimatorBuilder(string processorName, ChangesEstimationHandler estimationDelegate, TimeSpan? estimationPeriod = null!) => throw new NotImplementedException();
    public override FeedIterator<T> GetChangeFeedIterator<T>(ChangeFeedStartFrom changeFeedStartFrom, ChangeFeedMode changeFeedMode, ChangeFeedRequestOptions changeFeedRequestOptions = null!) => throw new NotImplementedException();
    public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder<T>(string processorName, ChangesHandler<T> onChangesDelegate) => throw new NotImplementedException();
    public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder<T>(string processorName, ChangeFeedHandler<T> onChangesDelegate) => throw new NotImplementedException();
    public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder(string processorName, ChangeFeedStreamHandler onChangesDelegate) => throw new NotImplementedException();
    public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilderWithManualCheckpoint<T>(string processorName, ChangeFeedHandlerWithManualCheckpoint<T> onChangesDelegate) => throw new NotImplementedException();
    public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilderWithManualCheckpoint(string processorName, ChangeFeedStreamHandlerWithManualCheckpoint onChangesDelegate) => throw new NotImplementedException();
    public override FeedIterator GetChangeFeedStreamIterator(ChangeFeedStartFrom changeFeedStartFrom, ChangeFeedMode changeFeedMode, ChangeFeedRequestOptions changeFeedRequestOptions = null!) => throw new NotImplementedException();
    public override Task<IReadOnlyList<FeedRange>> GetFeedRangesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override FeedIterator<T> GetItemQueryIterator<T>(QueryDefinition queryDefinition, string continuationToken = null!, QueryRequestOptions requestOptions = null!) => throw new NotImplementedException();
    public override FeedIterator<T> GetItemQueryIterator<T>(string queryText = null!, string continuationToken = null!, QueryRequestOptions requestOptions = null!) => GetItemQueryIterator<T>(queryText != null ? new QueryDefinition(queryText) : null!, continuationToken, requestOptions);
    public override FeedIterator<T> GetItemQueryIterator<T>(FeedRange feedRange, QueryDefinition queryDefinition, string continuationToken = null!, QueryRequestOptions requestOptions = null!) => throw new NotImplementedException();
    public override FeedIterator GetItemQueryStreamIterator(QueryDefinition queryDefinition, string continuationToken = null!, QueryRequestOptions requestOptions = null!) => throw new NotImplementedException();
    public override FeedIterator GetItemQueryStreamIterator(string queryText = null!, string continuationToken = null!, QueryRequestOptions requestOptions = null!) => throw new NotImplementedException();
    public override FeedIterator GetItemQueryStreamIterator(FeedRange feedRange, QueryDefinition queryDefinition, string continuationToken = null!, QueryRequestOptions requestOptions = null!) => throw new NotImplementedException();
    public override Task<ItemResponse<T>> PatchItemAsync<T>(string id, PartitionKey partitionKey, IReadOnlyList<PatchOperation> patchOperations, PatchItemRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ResponseMessage> PatchItemStreamAsync(string id, PartitionKey partitionKey, IReadOnlyList<PatchOperation> patchOperations, PatchItemRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ResponseMessage> ReadContainerStreamAsync(ContainerRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ResponseMessage> ReadItemStreamAsync(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ResponseMessage> ReadManyItemsStreamAsync(IReadOnlyList<(string id, PartitionKey partitionKey)> items, ReadManyRequestOptions readManyRequestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<int?> ReadThroughputAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ThroughputResponse> ReadThroughputAsync(RequestOptions requestOptions, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ContainerResponse> ReplaceContainerAsync(ContainerProperties containerProperties, ContainerRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ResponseMessage> ReplaceContainerStreamAsync(ContainerProperties containerProperties, ContainerRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ResponseMessage> ReplaceItemStreamAsync(Stream streamPayload, string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ThroughputResponse> ReplaceThroughputAsync(int throughput, RequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ThroughputResponse> ReplaceThroughputAsync(ThroughputProperties throughputProperties, RequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public override Task<ResponseMessage> UpsertItemStreamAsync(Stream streamPayload, PartitionKey partitionKey, ItemRequestOptions requestOptions = null!, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    #endregion
}
