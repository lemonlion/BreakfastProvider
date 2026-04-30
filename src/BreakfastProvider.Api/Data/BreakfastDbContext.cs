using BreakfastProvider.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakfastProvider.Api.Data;

public class BreakfastDbContext(DbContextOptions<BreakfastDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
    public DbSet<StaffMember> StaffMembers { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Quantity).HasPrecision(10, 2);
            entity.Property(e => e.ReorderLevel).HasPrecision(10, 2);
        });

        modelBuilder.Entity<StaffMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TableNumber, e.ReservedAt });
            entity.Property(e => e.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
        });
    }
}
