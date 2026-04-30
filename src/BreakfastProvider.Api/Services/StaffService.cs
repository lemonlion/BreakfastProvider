using BreakfastProvider.Api.Data;
using BreakfastProvider.Api.Data.Entities;
using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace BreakfastProvider.Api.Services;

public class StaffService(
    IDbContextFactory<BreakfastDbContext> dbContextFactory,
    PubSubEventPublisher<StaffMemberAddedEvent> staffAddedPublisher,
    ILogger<StaffService> logger) : IStaffService
{
    public async Task<StaffMemberResponse> CreateAsync(StaffMemberRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("StaffService.Create");

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new StaffMember
        {
            Name = request.Name!,
            Role = request.Role!,
            Email = request.Email!,
            IsActive = request.IsActive,
            HiredAt = request.HiredAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        db.StaffMembers.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Staff member '{Name}' created with ID {Id}", entity.Name, entity.Id);

        await staffAddedPublisher.PublishEvent(new StaffMemberAddedEvent
        {
            StaffId = entity.Id,
            Name = entity.Name,
            Role = entity.Role,
            AddedAt = entity.CreatedAt
        }, cancellationToken);

        return MapToResponse(entity);
    }

    public async Task<StaffMemberResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.StaffMembers.FindAsync([id], cancellationToken);
        return entity is null ? null : MapToResponse(entity);
    }

    public async Task<List<StaffMemberResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var items = await db.StaffMembers.OrderBy(s => s.Name).ToListAsync(cancellationToken);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<StaffMemberResponse?> UpdateAsync(int id, StaffMemberRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("StaffService.Update");

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.StaffMembers.FindAsync([id], cancellationToken);
        if (entity is null) return null;

        entity.Name = request.Name!;
        entity.Role = request.Role!;
        entity.Email = request.Email!;
        entity.IsActive = request.IsActive;
        entity.HiredAt = request.HiredAt ?? entity.HiredAt;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Staff member '{Name}' (ID {Id}) updated", entity.Name, entity.Id);
        return MapToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.StaffMembers.FindAsync([id], cancellationToken);
        if (entity is null) return false;

        db.StaffMembers.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Staff member '{Name}' (ID {Id}) deleted", entity.Name, entity.Id);
        return true;
    }

    private static StaffMemberResponse MapToResponse(StaffMember entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Role = entity.Role,
        Email = entity.Email,
        IsActive = entity.IsActive,
        HiredAt = entity.HiredAt,
        CreatedAt = entity.CreatedAt
    };
}
