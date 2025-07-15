using Microsoft.EntityFrameworkCore;
using ReportGen.Models;

namespace ReportGen.Data;

public class ReportGenDbContext : DbContext
{
    public ReportGenDbContext(DbContextOptions<ReportGenDbContext> options)
        : base(options)
    {
    }

    public DbSet<Value> Values { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Value>()
            .HasOne(v => v.Result)
            .WithMany(r => r.Values)
            .HasForeignKey(v => v.FileName)
            .HasPrincipalKey(r => r.FileName)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Value>()
            .Property(v => v.FileName)
            .IsRequired()
            .HasMaxLength(255);

        modelBuilder.Entity<Value>()
            .Property(v => v.StoreValue)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Result>()
            .Property(r => r.FileName)
            .IsRequired()
            .HasMaxLength(255);

        modelBuilder.Entity<Result>()
            .Property(r => r.AvgExecutionTime)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Result>()
            .Property(r => r.AvgStoreValue)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Result>()
            .Property(r => r.MedianStoreValue)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Result>()
            .Property(r => r.MaximumStoreValue)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Result>()
            .Property(r => r.MinimumStoreValue)
            .HasColumnType("decimal(18,4)");

        base.OnModelCreating(modelBuilder);
    }
}