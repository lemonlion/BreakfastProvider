using Confluent.Kafka;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;

public abstract class NotImplementedClient : IClient
{
    public void Dispose()
    {
        // do nothing
    }

    public int AddBrokers(string brokers) => throw new NotImplementedException();

    public void SetSaslCredentials(string username, string password) => throw new NotImplementedException();

    public Handle Handle { get; } = null;
    public string Name { get; } = null;
}
