using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Cosmos;

/// <summary>
/// In-memory implementation of <see cref="TransactionalBatch"/> that collects
/// create operations and applies them atomically to the <see cref="InMemoryContainer"/>
/// backing store on <see cref="ExecuteAsync"/>.
/// </summary>
public class InMemoryTransactionalBatch(
    ConcurrentDictionary<string, string> items,
    JsonSerializerOptions jsonOptions) : TransactionalBatch
{
    private readonly List<(string Id, string Json)> _pendingCreates = [];

    public override TransactionalBatch CreateItem<T>(T item, TransactionalBatchItemRequestOptions? requestOptions = null)
    {
        var id = GetId(item);
        var json = JsonSerializer.Serialize(item, jsonOptions);
        _pendingCreates.Add((id, json));
        return this;
    }

    public override Task<TransactionalBatchResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(null, cancellationToken);
    }

    public override Task<TransactionalBatchResponse> ExecuteAsync(
        TransactionalBatchRequestOptions? requestOptions,
        CancellationToken cancellationToken = default)
    {
        // Check all items can be created (no duplicates)
        foreach (var (id, _) in _pendingCreates)
        {
            if (items.ContainsKey(id))
            {
                var failResponse = Substitute.For<TransactionalBatchResponse>();
                failResponse.StatusCode.Returns(HttpStatusCode.Conflict);
                failResponse.IsSuccessStatusCode.Returns(false);
                failResponse.ActivityId.Returns(string.Empty);
                failResponse.RequestCharge.Returns(0d);
                return Task.FromResult(failResponse);
            }
        }

        // Apply all creates atomically
        foreach (var (id, json) in _pendingCreates)
        {
            if (!items.TryAdd(id, json))
            {
                // Shouldn't happen since we checked above, but roll back on conflict
                foreach (var (prevId, _) in _pendingCreates)
                {
                    if (prevId == id) break;
                    items.TryRemove(prevId, out _);
                }

                var failResponse = Substitute.For<TransactionalBatchResponse>();
                failResponse.StatusCode.Returns(HttpStatusCode.Conflict);
                failResponse.IsSuccessStatusCode.Returns(false);
                failResponse.ActivityId.Returns(string.Empty);
                failResponse.RequestCharge.Returns(0d);
                return Task.FromResult(failResponse);
            }
        }

        var response = Substitute.For<TransactionalBatchResponse>();
        response.StatusCode.Returns(HttpStatusCode.OK);
        response.IsSuccessStatusCode.Returns(true);
        response.ActivityId.Returns(string.Empty);
        response.RequestCharge.Returns(0d);
        return Task.FromResult(response);
    }

    private static string GetId<T>(T item)
    {
        var idProp = typeof(T).GetProperty("Id");
        return idProp?.GetValue(item)?.ToString() ?? Guid.NewGuid().ToString();
    }

    #region Unsupported operations

    public override TransactionalBatch CreateItemStream(Stream streamPayload, TransactionalBatchItemRequestOptions? requestOptions = null) => throw new NotImplementedException();
    public override TransactionalBatch DeleteItem(string id, TransactionalBatchItemRequestOptions? requestOptions = null) => throw new NotImplementedException();
    public override TransactionalBatch PatchItem(string id, IReadOnlyList<PatchOperation> patchOperations, TransactionalBatchPatchItemRequestOptions? requestOptions = null) => throw new NotImplementedException();
    public override TransactionalBatch ReadItem(string id, TransactionalBatchItemRequestOptions? requestOptions = null) => throw new NotImplementedException();
    public override TransactionalBatch ReplaceItem<T>(string id, T item, TransactionalBatchItemRequestOptions? requestOptions = null) => throw new NotImplementedException();
    public override TransactionalBatch ReplaceItemStream(string id, Stream streamPayload, TransactionalBatchItemRequestOptions? requestOptions = null) => throw new NotImplementedException();
    public override TransactionalBatch UpsertItem<T>(T item, TransactionalBatchItemRequestOptions? requestOptions = null) => throw new NotImplementedException();
    public override TransactionalBatch UpsertItemStream(Stream streamPayload, TransactionalBatchItemRequestOptions? requestOptions = null) => throw new NotImplementedException();

    #endregion
}
