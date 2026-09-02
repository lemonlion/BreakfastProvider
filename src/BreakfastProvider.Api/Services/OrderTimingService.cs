using BreakfastProvider.Api.Data.ClickHouse;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Services;

public interface IOrderTimingService
{
    Task<OrderTimingResponse> RecordAsync(OrderTimingRequest request, CancellationToken cancellationToken = default);
    Task<List<OrderTimingSummaryResponse>> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<List<OrderTimingResponse>> ListByStationAsync(string station, CancellationToken cancellationToken = default);
}

/// <summary>
/// Records kitchen order timings in ClickHouse. Codes against ADO.NET abstractions only
/// (<c>DbConnection</c> / <c>DbCommand</c> / <c>DbDataReader</c>) so that tests can wrap the
/// connection in a tracking decorator with no change here.
/// </summary>
public class OrderTimingService(IClickHouseConnectionFactory connectionFactory, ILogger<OrderTimingService> logger) : IOrderTimingService
{
    private const string TableName = "order_timings";

    public async Task<OrderTimingResponse> RecordAsync(OrderTimingRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("OrderTimingService.Record");

        var timingId = Guid.NewGuid().ToString();
        var recordedAt = DateTime.UtcNow;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"INSERT INTO {TableName} (timing_id, order_id, station, item_type, prep_seconds, recorded_at) " +
            "VALUES ({timingId:String}, {orderId:String}, {station:String}, {itemType:String}, {prepSeconds:Float64}, {recordedAt:DateTime})";
        command
            .AddParameter("timingId", timingId)
            .AddParameter("orderId", request.OrderId!)
            .AddParameter("station", request.Station!)
            .AddParameter("itemType", request.ItemType!)
            .AddParameter("prepSeconds", (double)request.PrepSeconds)
            .AddParameter("recordedAt", recordedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Order timing {TimingId} recorded for station {Station}", timingId, request.Station);

        return new OrderTimingResponse
        {
            TimingId = timingId,
            OrderId = request.OrderId!,
            Station = request.Station!,
            ItemType = request.ItemType!,
            PrepSeconds = request.PrepSeconds,
            RecordedAt = recordedAt
        };
    }

    public async Task<List<OrderTimingSummaryResponse>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("OrderTimingService.GetSummary");

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT station, avg(prep_seconds) AS avg_prep_seconds, quantile(0.95)(prep_seconds) AS p95_prep_seconds, count() AS timing_count " +
            $"FROM {TableName} GROUP BY station ORDER BY avg_prep_seconds DESC";

        var results = new List<OrderTimingSummaryResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OrderTimingSummaryResponse
            {
                Station = reader.GetString(reader.GetOrdinal("station")),
                AvgPrepSeconds = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("avg_prep_seconds"))),
                P95PrepSeconds = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("p95_prep_seconds"))),
                TimingCount = Convert.ToInt32(reader["timing_count"])
            });
        }

        return results;
    }

    public async Task<List<OrderTimingResponse>> ListByStationAsync(string station, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("OrderTimingService.ListByStation");

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT timing_id, order_id, station, item_type, prep_seconds, recorded_at " +
            $"FROM {TableName} WHERE station = {{station:String}} ORDER BY recorded_at DESC";
        command.AddParameter("station", station);

        var results = new List<OrderTimingResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OrderTimingResponse
            {
                TimingId = reader.GetString(reader.GetOrdinal("timing_id")),
                OrderId = reader.GetString(reader.GetOrdinal("order_id")),
                Station = reader.GetString(reader.GetOrdinal("station")),
                ItemType = reader.GetString(reader.GetOrdinal("item_type")),
                PrepSeconds = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("prep_seconds"))),
                RecordedAt = reader.GetDateTime(reader.GetOrdinal("recorded_at"))
            });
        }

        return results;
    }
}
