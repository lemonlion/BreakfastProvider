using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Api.Services;
using BreakfastProvider.Api.Telemetry;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace BreakfastProvider.Api.Reporting;

public interface ICustomerFeedbackAlertService
{
    Task ProcessFeedbackAsync(Guid feedbackId, string customerName, string recipeName, int rating, string comments, DateTime receivedAt, CancellationToken cancellationToken = default);
}

public class CustomerFeedbackAlertService(
    IMongoClient mongoClient,
    INotificationClient notificationClient,
    IHttpClientFactory httpClientFactory,
    ILogger<CustomerFeedbackAlertService> logger) : ICustomerFeedbackAlertService
{
    private IMongoCollection<CustomerFeedbackAlertDocument> Collection =>
        mongoClient.GetDatabase("BreakfastDb").GetCollection<CustomerFeedbackAlertDocument>("feedback_alerts");

    public async Task ProcessFeedbackAsync(Guid feedbackId, string customerName, string recipeName, int rating, string comments, DateTime receivedAt, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("CustomerFeedbackAlertService.ProcessFeedback");

        // Step 1: Write to MongoDB
        var doc = new CustomerFeedbackAlertDocument
        {
            AlertId = feedbackId.ToString(),
            CustomerName = customerName,
            RecipeName = recipeName,
            Rating = rating,
            Comments = comments,
            ReceivedAt = receivedAt,
            ProcessedAt = DateTime.UtcNow
        };

        await Collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        logger.LogInformation("Feedback alert {FeedbackId} stored in MongoDB for recipe {RecipeName}", feedbackId, recipeName);

        // Step 2: Send notification via gRPC
        var (success, notificationId) = await notificationClient.SendOrderConfirmationAsync(
            feedbackId.ToString(), customerName, rating, cancellationToken);

        logger.LogInformation("Notification sent for feedback {FeedbackId}: Success={Success}, NotificationId={NotificationId}",
            feedbackId, success, notificationId);

        // Step 3: Call Supplier Service to log the feedback
        var supplierClient = httpClientFactory.CreateClient(HttpClientNames.SupplierService);
        await supplierClient.PostAsJsonAsync("ingredients/feedback", new
        {
            FeedbackId = feedbackId,
            RecipeName = recipeName,
            Rating = rating,
            CustomerName = customerName
        }, cancellationToken);

        logger.LogInformation("Supplier service notified about feedback {FeedbackId} for recipe {RecipeName}", feedbackId, recipeName);
    }
}

public class CustomerFeedbackAlertDocument
{
    [BsonId]
    public string AlertId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comments { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public DateTime ProcessedAt { get; set; }
}
