using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Models.ServiceTimes;
using Confluent.Kafka;
using Kronikol.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Common.ServiceTimes;

public class PublishOrderServedEventSteps(
    ConsumedKafkaMessageStore kafkaStore,
    RequestContext context)
{
    public TestOrderServedRequest Request { get; set; } = new();
    public Guid ServiceId { get; private set; }
    public Guid OrderId { get; private set; }

    public Task PublishEvent()
    {
        ServiceId = Guid.NewGuid();
        OrderId = Request.OrderId ?? Guid.NewGuid();

        var @event = new OrderServedEvent
        {
            ServiceId = ServiceId,
            OrderId = OrderId,
            ItemType = Request.ItemType!,
            WaitSeconds = Request.WaitSeconds,
            ServedAt = DateTime.UtcNow
        };

        // The in-memory consumer handles the message synchronously inside this scope, so the
        // ClickHouse / gRPC / Kitchen arrows are attributed to this test with no HTTP request in flight.
        using (TestIdentityScope.Begin("ServiceTimeTest", context.RequestId))
        {
            var message = new Message<string, string>
            {
                Key = ServiceId.ToString(),
                Value = JsonSerializer.Serialize(@event),
                Headers = new Headers
                {
                    { "kronikol-test-name", System.Text.Encoding.UTF8.GetBytes("ServiceTimeTest") },
                    { "kronikol-test-id", System.Text.Encoding.UTF8.GetBytes(context.RequestId) }
                }
            };

            kafkaStore.Add(message, "OrderServedEvent");
        }

        return Task.CompletedTask;
    }

    private class OrderServedEvent
    {
        public Guid ServiceId { get; set; }
        public Guid OrderId { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public decimal WaitSeconds { get; set; }
        public DateTime ServedAt { get; set; }
    }
}
