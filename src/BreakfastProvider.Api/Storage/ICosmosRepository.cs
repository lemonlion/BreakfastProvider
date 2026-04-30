using System.Linq.Expressions;

namespace BreakfastProvider.Api.Storage;

public interface ICosmosRepository<T> where T : class
{
    Task<T> CreateAsync(T item, string partitionKey, CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, int, bool>> predicate, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<T> Items, int TotalCount)> QueryPagedAsync(Expression<Func<T, bool>> predicate, int offset, int limit, CancellationToken cancellationToken = default);
    Task<T> UpsertAsync(T item, string partitionKey, CancellationToken cancellationToken = default);
}
