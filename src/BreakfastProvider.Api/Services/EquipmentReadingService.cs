using BreakfastProvider.Api.Data.ClickHouse;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Services;

public interface IEquipmentReadingService
{
    Task<EquipmentReadingResponse> RecordAsync(EquipmentReadingRequest request, CancellationToken cancellationToken = default);
    Task<List<EquipmentReadingResponse>> ListByEquipmentAsync(string equipmentId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string readingId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Records kitchen equipment readings in ClickHouse. Codes against ADO.NET abstractions only
/// so that tests can wrap the connection in a tracking decorator with no change here.
/// </summary>
public class EquipmentReadingService(IClickHouseConnectionFactory connectionFactory, ILogger<EquipmentReadingService> logger) : IEquipmentReadingService
{
    private const string TableName = "equipment_readings";

    public async Task<EquipmentReadingResponse> RecordAsync(EquipmentReadingRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("EquipmentReadingService.Record");

        var readingId = Guid.NewGuid().ToString();
        var recordedAt = DateTime.UtcNow;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"INSERT INTO {TableName} (reading_id, equipment_id, metric, value, unit, recorded_at) " +
            "VALUES ({readingId:String}, {equipmentId:String}, {metric:String}, {value:Float64}, {unit:String}, {recordedAt:DateTime})";
        command
            .AddParameter("readingId", readingId)
            .AddParameter("equipmentId", request.EquipmentId!)
            .AddParameter("metric", request.Metric!)
            .AddParameter("value", (double)request.Value)
            .AddParameter("unit", request.Unit!)
            .AddParameter("recordedAt", recordedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Equipment reading {ReadingId} recorded for {EquipmentId} ({Metric})",
            readingId, request.EquipmentId, request.Metric);

        return new EquipmentReadingResponse
        {
            ReadingId = readingId,
            EquipmentId = request.EquipmentId!,
            Metric = request.Metric!,
            Value = request.Value,
            Unit = request.Unit!,
            RecordedAt = recordedAt
        };
    }

    public async Task<List<EquipmentReadingResponse>> ListByEquipmentAsync(string equipmentId, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("EquipmentReadingService.ListByEquipment");

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT reading_id, equipment_id, metric, value, unit, recorded_at " +
            $"FROM {TableName} WHERE equipment_id = {{equipmentId:String}} ORDER BY recorded_at DESC";
        command.AddParameter("equipmentId", equipmentId);

        var results = new List<EquipmentReadingResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new EquipmentReadingResponse
            {
                ReadingId = reader.GetString(reader.GetOrdinal("reading_id")),
                EquipmentId = reader.GetString(reader.GetOrdinal("equipment_id")),
                Metric = reader.GetString(reader.GetOrdinal("metric")),
                Value = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("value"))),
                Unit = reader.GetString(reader.GetOrdinal("unit")),
                // The column is DateTime (UTC wall-clock); ClickHouse.Driver reads it back with
                // Kind=Unspecified, which would serialize without the trailing 'Z'.
                RecordedAt = DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("recorded_at")), DateTimeKind.Utc)
            });
        }

        return results;
    }

    public async Task<bool> DeleteAsync(string readingId, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("EquipmentReadingService.Delete");

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // Lightweight DELETE: with ClickHouse 25.8 defaults (lightweight_deletes_sync=2) the row
        // is gone from the next SELECT, unlike the asynchronous ALTER TABLE ... DELETE mutation.
        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {TableName} WHERE reading_id = {{readingId:String}}";
        command.AddParameter("readingId", readingId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Equipment reading {ReadingId} deleted", readingId);
        return true;
    }
}
