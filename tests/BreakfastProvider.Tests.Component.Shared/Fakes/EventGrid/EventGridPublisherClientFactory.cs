using System.Net.Security;
using Azure.Messaging.EventGrid;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.EventGrid;

/// <summary>
/// Creates <see cref="EventGridPublisherClientOptions"/> configured with a shared
/// <see cref="SocketsHttpHandler"/> that trusts self-signed certificates.
/// The shared handler pools TLS connections to the Docker EventGrid simulator,
/// preventing concurrent handshake contention that hangs under parallel tests.
/// </summary>
public static class EventGridPublisherClientFactory
{
    private static readonly SocketsHttpHandler SharedHandler = new()
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        },
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        ConnectTimeout = TimeSpan.FromSeconds(10)
    };

    public static EventGridPublisherClientOptions CreateOptions()
    {
        var options = new EventGridPublisherClientOptions();
        options.Transport = new Azure.Core.Pipeline.HttpClientTransport(new HttpClient(SharedHandler, disposeHandler: false));
        return options;
    }
}
