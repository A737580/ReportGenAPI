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
    public DbSet<ScalarDecimalResult> ScalarDecimalResults { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
            modelBuilder.Entity<Value>(entity =>
        {
              entity.Property(e => e.FileName)
                .IsRequired();

              entity.Property(e => e.FileName)
                .HasMaxLength(255);

              entity.Property(e => e.StoreValue)
                .HasColumnType("decimal(18,4)");

              entity.HasOne(v => v.Result)
                .WithMany(r => r.Values)
                .HasForeignKey(v => v.FileName)
                .HasPrincipalKey(r => r.FileName);
        });

            modelBuilder.Entity<Result>(entity =>
            {
                  entity.HasKey(e => e.FileName);

                  entity.Property(e => e.FileName)
                .HasMaxLength(255);

                  entity.Property(e => e.AvgExecutionTime)
                .HasColumnType("decimal(18,4)");

                  entity.Property(e => e.AvgStoreValue)
                .HasColumnType("decimal(18,4)");

                  entity.Property(e => e.MedianStoreValue)
                .HasColumnType("decimal(18,4)");

                  entity.Property(e => e.MaximumStoreValue)
                .HasColumnType("decimal(18,4)");

                  entity.Property(e => e.MinimumStoreValue)
                .HasColumnType("decimal(18,4)");
            });

            modelBuilder.Entity<ScalarDecimalResult>().HasNoKey(); 
            modelBuilder.Entity<ScalarDecimalResult>().ToView(null);

            base.OnModelCreating(modelBuilder);
      }
}
