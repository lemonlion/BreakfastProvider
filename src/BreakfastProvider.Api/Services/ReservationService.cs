using BreakfastProvider.Api.Data;
using BreakfastProvider.Api.Data.Entities;
using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace BreakfastProvider.Api.Services;

public class ReservationService(
    IDbContextFactory<BreakfastDbContext> dbContextFactory,
    PubSubEventPublisher<ReservationConfirmedEvent> confirmedPublisher,
    PubSubEventPublisher<ReservationCancelledEvent> cancelledPublisher,
    INotificationClient notificationClient,
    ILogger<ReservationService> logger) : IReservationService
{
    public async Task<ReservationResponse> CreateAsync(ReservationRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ReservationService.Create");

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new Reservation
        {
            CustomerName = request.CustomerName!,
            TableNumber = request.TableNumber,
            PartySize = request.PartySize,
            ReservedAt = request.ReservedAt,
            Status = "Confirmed",
            ContactPhone = request.ContactPhone,
            CreatedAt = DateTime.UtcNow
        };

        db.Reservations.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reservation for '{CustomerName}' created with ID {Id} at table {TableNumber}",
            entity.CustomerName, entity.Id, entity.TableNumber);

        await confirmedPublisher.PublishEvent(new ReservationConfirmedEvent
        {
            ReservationId = entity.Id,
            CustomerName = entity.CustomerName,
            PartySize = entity.PartySize,
            ReservedAt = entity.ReservedAt,
            ConfirmedAt = entity.CreatedAt
        }, cancellationToken);

        // Send reservation reminder via gRPC — fire-and-forget.
        try
        {
            await notificationClient.SendReservationReminderAsync(
                entity.Id.ToString(), entity.CustomerName, entity.ReservedAt, entity.TableNumber, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reservation reminder notification failed for reservation {ReservationId}; reservation is committed", entity.Id);
        }

        return MapToResponse(entity);
    }

    public async Task<ReservationResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Reservations.FindAsync([id], cancellationToken);
        return entity is null ? null : MapToResponse(entity);
    }

    public async Task<List<ReservationResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var items = await db.Reservations.OrderBy(r => r.ReservedAt).ToListAsync(cancellationToken);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<ReservationResponse?> UpdateAsync(int id, ReservationRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ReservationService.Update");

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Reservations.FindAsync([id], cancellationToken);
        if (entity is null) return null;

        if (entity.Status == "Cancelled")
            return null;

        entity.CustomerName = request.CustomerName!;
        entity.TableNumber = request.TableNumber;
        entity.PartySize = request.PartySize;
        entity.ReservedAt = request.ReservedAt;
        entity.ContactPhone = request.ContactPhone;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reservation ID {Id} updated", entity.Id);
        return MapToResponse(entity);
    }

    public async Task<(ReservationResponse? Reservation, string? Error)> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Reservations.FindAsync([id], cancellationToken);
        if (entity is null) return (null, null);

        if (entity.Status == "Cancelled")
            return (null, "Reservation is already cancelled.");

        entity.Status = "Cancelled";
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reservation ID {Id} cancelled", entity.Id);

        await cancelledPublisher.PublishEvent(new ReservationCancelledEvent
        {
            ReservationId = entity.Id,
            CustomerName = entity.CustomerName,
            Reason = "Cancelled by customer",
            CancelledAt = DateTime.UtcNow
        }, cancellationToken);

        return (MapToResponse(entity), null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Reservations.FindAsync([id], cancellationToken);
        if (entity is null) return false;

        db.Reservations.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reservation ID {Id} deleted", entity.Id);
        return true;
    }

    private static ReservationResponse MapToResponse(Reservation entity) => new()
    {
        Id = entity.Id,
        CustomerName = entity.CustomerName,
        TableNumber = entity.TableNumber,
        PartySize = entity.PartySize,
        ReservedAt = entity.ReservedAt,
        Status = entity.Status,
        ContactPhone = entity.ContactPhone,
        CreatedAt = entity.CreatedAt
    };
}
