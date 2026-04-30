using System.Collections.Concurrent;

namespace BreakfastProvider.Tests.Component.LightBDD.Fakes.HttpFakes;

public class FakeRequestStore
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<CapturedHttpRequest>> _requests = new();

    public void Add(string requestId, CapturedHttpRequest request)
    {
        _requests.GetOrAdd(requestId, _ => []).Add(request);
    }

    public IReadOnlyList<CapturedHttpRequest> GetRequests(string requestId)
    {
        return _requests.TryGetValue(requestId, out var bag) ? bag.ToList() : [];
    }

    public IReadOnlyList<CapturedHttpRequest> GetRequests(string requestId, string clientName)
    {
        return GetRequests(requestId)
            .Where(r => r.ClientName.Equals(clientName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

public record CapturedHttpRequest(
    string ClientName,
    HttpMethod Method,
    Uri? RequestUri,
    Dictionary<string, string> Headers,
    string? Body);