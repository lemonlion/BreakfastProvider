using Microsoft.EntityFrameworkCore;

namespace BreakfastProvider.Api.Reporting;

public class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public DbSet<OrderSummary> OrderSummaries { get; set; } = null!;
    public DbSet<RecipeReport> RecipeReports { get; set; } = null!;
    public DbSet<BatchCompletionRecord> BatchCompletionRecords { get; set; } = null!;
    public DbSet<IngredientShipment> IngredientShipments { get; set; } = null!;
    public DbSet<EquipmentAlert> EquipmentAlerts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderSummary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.Property(e => e.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<RecipeReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.Property(e => e.RecipeType).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<BatchCompletionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BatchId);
            entity.Property(e => e.RecipeType).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<IngredientShipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeliveryId);
            entity.Property(e => e.IngredientName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<EquipmentAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AlertId);
            entity.Property(e => e.EquipmentName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AlertType).HasMaxLength(100).IsRequired();
        });
    }
}
