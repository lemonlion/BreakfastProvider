using Newtonsoft.Json;

namespace BreakfastProvider.Api.Storage;

public class OrderDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderItemDocument> Items { get; set; } = [];
    public int? TableNumber { get; set; }
    public string Status { get; set; } = "Created";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrderItemDocument
{
    public string ItemType { get; set; } = string.Empty;
    public Guid BatchId { get; set; }
    public int Quantity { get; set; }
}

public class RecipeDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string RecipeType { get; set; } = string.Empty;
    public List<string> Ingredients { get; set; } = [];
    public List<string> Toppings { get; set; } = [];
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}

public class AuditLogDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;
    public Guid AuditLogId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class OutboxMessage
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = OutboxMessageStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public static class OutboxMessageStatus
{
    public const string Pending = "Pending";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
}

public class IdempotencyRecord
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;
    public string ResponsePayload { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Ttl { get; set; }
}
