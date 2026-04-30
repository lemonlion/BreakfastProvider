using System.Net;
using System.Net.Sockets;

namespace BreakfastProvider.Tests.Component.Shared.Infrastructure.Hosting;

public static class HttpFakesHelper
{
    public static void AssertPortIsNotInUse(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Url '{url}' is an invalid format.");

        IPAddress? ipAddress = null;
        if (url.Contains("localhost"))
            ipAddress = Dns.GetHostEntry("localhost").AddressList[0];

        try
        {
            var tcpListener = new TcpListener(ipAddress!, uri.Port);
            tcpListener.Start();
            tcpListener.Stop();
        }
        catch (SocketException)
        {
            throw new InvalidOperationException(
                $"Url '{url}' is currently in use. " +
                "Please check that another service is not bound to this address. " +
                "If you have fakes running in Docker, close those containers or set RunWithAnInMemory*=false.");
        }
    }
}
