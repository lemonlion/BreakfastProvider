using BreakfastProvider.Api.Data.Spanner;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using Google.Cloud.Spanner.Data;

namespace BreakfastProvider.Api.Services;

public interface IFeedbackService
{
    Task<FeedbackResponse> CreateAsync(FeedbackRequest request, CancellationToken cancellationToken = default);
    Task<FeedbackResponse?> GetByIdAsync(string feedbackId, CancellationToken cancellationToken = default);
    Task<List<FeedbackResponse>> ListByOrderAsync(string orderId, CancellationToken cancellationToken = default);
}

public class FeedbackService(ISpannerConnectionFactory connectionFactory, ILogger<FeedbackService> logger) : IFeedbackService
{
    public async Task<FeedbackResponse> CreateAsync(FeedbackRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("FeedbackService.Create");

        var feedbackId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateInsertCommand("Feedback");
        cmd.Parameters.Add("FeedbackId", SpannerDbType.String, feedbackId);
        cmd.Parameters.Add("CustomerName", SpannerDbType.String, request.CustomerName);
        cmd.Parameters.Add("OrderId", SpannerDbType.String, request.OrderId);
        cmd.Parameters.Add("Rating", SpannerDbType.Int64, (long)request.Rating);
        cmd.Parameters.Add("Comment", SpannerDbType.String, request.Comment ?? string.Empty);
        cmd.Parameters.Add("CreatedAt", SpannerDbType.Timestamp, now);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Feedback {FeedbackId} created for order {OrderId}", feedbackId, request.OrderId);

        return new FeedbackResponse
        {
            FeedbackId = feedbackId,
            CustomerName = request.CustomerName!,
            OrderId = request.OrderId!,
            Rating = request.Rating,
            Comment = request.Comment ?? string.Empty,
            CreatedAt = now
        };
    }

    public async Task<FeedbackResponse?> GetByIdAsync(string feedbackId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateSelectCommand(
            "SELECT FeedbackId, CustomerName, OrderId, Rating, Comment, CreatedAt FROM Feedback WHERE FeedbackId = @id");
        cmd.Parameters.Add("id", SpannerDbType.String, feedbackId);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapFromReader(reader);
    }

    public async Task<List<FeedbackResponse>> ListByOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateSelectCommand(
            "SELECT FeedbackId, CustomerName, OrderId, Rating, Comment, CreatedAt FROM Feedback WHERE OrderId = @orderId ORDER BY CreatedAt");
        cmd.Parameters.Add("orderId", SpannerDbType.String, orderId);

        var results = new List<FeedbackResponse>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapFromReader(reader));
        }

        return results;
    }

    private static FeedbackResponse MapFromReader(SpannerDataReader reader) => new()
    {
        FeedbackId = reader.GetFieldValue<string>("FeedbackId"),
        CustomerName = reader.GetFieldValue<string>("CustomerName"),
        OrderId = reader.GetFieldValue<string>("OrderId"),
        Rating = (int)reader.GetFieldValue<long>("Rating"),
        Comment = reader.GetFieldValue<string>("Comment"),
        CreatedAt = reader.GetFieldValue<DateTime>("CreatedAt")
    };
}
