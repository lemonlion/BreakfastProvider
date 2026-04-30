using BreakfastProvider.Api.Data.Spanner;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using Google.Cloud.Spanner.Data;

namespace BreakfastProvider.Api.Services;

public interface ICustomerPreferenceService
{
    Task<CustomerPreferenceResponse> UpsertAsync(CustomerPreferenceRequest request, CancellationToken cancellationToken = default);
    Task<CustomerPreferenceResponse?> GetByIdAsync(string customerId, CancellationToken cancellationToken = default);
}

public class CustomerPreferenceService(ISpannerConnectionFactory connectionFactory, ILogger<CustomerPreferenceService> logger) : ICustomerPreferenceService
{
    public async Task<CustomerPreferenceResponse> UpsertAsync(CustomerPreferenceRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("CustomerPreferenceService.Upsert");

        var now = DateTime.UtcNow;

        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // Spanner InsertOrUpdate (upsert)
        var cmd = connection.CreateInsertOrUpdateCommand("CustomerPreferences");
        cmd.Parameters.Add("CustomerId", SpannerDbType.String, request.CustomerId);
        cmd.Parameters.Add("CustomerName", SpannerDbType.String, request.CustomerName);
        cmd.Parameters.Add("PreferredMilkType", SpannerDbType.String, request.PreferredMilkType ?? "standard");
        cmd.Parameters.Add("LikesExtraToppings", SpannerDbType.Bool, request.LikesExtraToppings);
        cmd.Parameters.Add("FavouriteItem", SpannerDbType.String, request.FavouriteItem ?? string.Empty);
        cmd.Parameters.Add("UpdatedAt", SpannerDbType.Timestamp, now);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Customer preference for {CustomerId} upserted", request.CustomerId);

        return new CustomerPreferenceResponse
        {
            CustomerId = request.CustomerId!,
            CustomerName = request.CustomerName!,
            PreferredMilkType = request.PreferredMilkType ?? "standard",
            LikesExtraToppings = request.LikesExtraToppings,
            FavouriteItem = request.FavouriteItem ?? string.Empty,
            UpdatedAt = now
        };
    }

    public async Task<CustomerPreferenceResponse?> GetByIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateSelectCommand(
            "SELECT CustomerId, CustomerName, PreferredMilkType, LikesExtraToppings, FavouriteItem, UpdatedAt FROM CustomerPreferences WHERE CustomerId = @id");
        cmd.Parameters.Add("id", SpannerDbType.String, customerId);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new CustomerPreferenceResponse
        {
            CustomerId = reader.GetFieldValue<string>("CustomerId"),
            CustomerName = reader.GetFieldValue<string>("CustomerName"),
            PreferredMilkType = reader.GetFieldValue<string>("PreferredMilkType"),
            LikesExtraToppings = reader.GetFieldValue<bool>("LikesExtraToppings"),
            FavouriteItem = reader.GetFieldValue<string>("FavouriteItem"),
            UpdatedAt = reader.GetFieldValue<DateTime>("UpdatedAt")
        };
    }
}
