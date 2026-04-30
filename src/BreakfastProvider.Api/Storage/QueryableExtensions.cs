using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace BreakfastProvider.Api.Storage;

public static class QueryableExtensions
{
    /// <summary>
    /// Optional factory override for creating a <see cref="FeedIterator{T}"/> from an
    /// <see cref="IQueryable{T}"/>. When <c>null</c> (default), the Cosmos SDK LINQ
    /// provider (<see cref="CosmosLinqExtensions.ToFeedIterator{T}"/>) is used.
    /// </summary>
    public static Func<object, object>? FeedIteratorFactory { get; set; }

    public static FeedIterator<T> ToOverridableFeedIterator<T>(this IQueryable<T> queryable)
        => FeedIteratorFactory is not null
            ? (FeedIterator<T>)FeedIteratorFactory(queryable)
            : queryable.ToFeedIterator();
}
