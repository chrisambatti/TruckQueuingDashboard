using Microsoft.EntityFrameworkCore;
using TruckQueuingDashboard.Domain.Entities;

namespace TruckQueuingDashboard.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext   // ← plain DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TBatchingplant> TBatchingplants { get; set; }
    public virtual DbSet<CallQueue> CallQueue { get; set; }

    // ─── (Optional) Log modifications ──────────────────────────────────
    public override int SaveChanges()
    {
        LogModifiedEntities();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        LogModifiedEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void LogModifiedEntities()
    {
        var modifiedEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified && e.Entity is TBatchingplant)
            .ToList();

        if (modifiedEntries.Any())
        {
            Console.WriteLine("⚠️ Modified TBatchingplant entities before SaveChanges:");
            foreach (var entry in modifiedEntries)
            {
                var entity = (TBatchingplant)entry.Entity;
                var originalCalledNow = entry.OriginalValues["CalledNow"]?.ToString() ?? "null";
                var currentCalledNow = entry.CurrentValues["CalledNow"]?.ToString() ?? "null";
                Console.WriteLine($"   Id: {entity.Id}, Vehicle: {entity.VehicleNumber}, " +
                                  $"Original CalledNow: {originalCalledNow}, " +
                                  $"Current CalledNow: {currentCalledNow}, " +
                                  $"State: {entry.State}");
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ─── Configure your tables only ────────────────────────────────
        modelBuilder.Entity<TBatchingplant>(entity =>
        {
            entity.ToTable("t_batchingplant");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CalledNow).HasDefaultValue(false);
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .HasColumnName("Created_by");
            entity.Property(e => e.Location)
                .HasMaxLength(100)
                .HasColumnName("location");
            entity.Property(e => e.RecordTimestamp).HasColumnType("datetime");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.Uid)
                .HasMaxLength(50)
                .HasColumnName("uid");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .HasColumnName("Updated_by");
            entity.Property(e => e.VehicleNumber)
                .HasMaxLength(50)
                .HasColumnName("vehicle_number");
        });

        modelBuilder.Entity<CallQueue>(entity =>
        {
            entity.ToTable("t_call_queue");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VehicleNumber).HasMaxLength(50);
            entity.Property(e => e.CalledAt).HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}