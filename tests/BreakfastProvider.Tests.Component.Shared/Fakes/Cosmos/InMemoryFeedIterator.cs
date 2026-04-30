using System.Net;
using Microsoft.Azure.Cosmos;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Cosmos;

/// <summary>
/// Wraps an in-memory list of items to satisfy <see cref="FeedIterator{T}"/>.
/// Returns all items in a single page then reports no more results.
/// </summary>
public sealed class InMemoryFeedIterator<T> : FeedIterator<T>
{
    private readonly IReadOnlyList<T> _items;
    private bool _hasMoreResults = true;

    public InMemoryFeedIterator(IReadOnlyList<T> items)
    {
        _items = items;
    }

    public override bool HasMoreResults => _hasMoreResults;

    public override Task<FeedResponse<T>> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        _hasMoreResults = false;
        return Task.FromResult<FeedResponse<T>>(new InMemoryFeedResponse<T>(_items));
    }

    private sealed class InMemoryFeedResponse<TItem> : FeedResponse<TItem>
    {
        private readonly IReadOnlyList<TItem> _items;

        public InMemoryFeedResponse(IReadOnlyList<TItem> items) => _items = items;

        public override Headers Headers { get; } = new();
        public override IEnumerable<TItem> Resource => _items;
        public override HttpStatusCode StatusCode => HttpStatusCode.OK;
        public override CosmosDiagnostics Diagnostics => null!;
        public override int Count => _items.Count;
        public override string IndexMetrics => null!;
        public override string ContinuationToken => null!;
        public override double RequestCharge => 0;
        public override string ActivityId => string.Empty;
        public override string ETag => null!;

        public override IEnumerator<TItem> GetEnumerator() => _items.GetEnumerator();
    }
}
