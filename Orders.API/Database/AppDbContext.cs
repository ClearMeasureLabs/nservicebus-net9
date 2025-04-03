using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Orders.API.Entities;

namespace Orders.API.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var relationalOptions = RelationalOptionsExtension.Extract(optionsBuilder.Options);
        relationalOptions.WithMigrationsHistoryTableSchema("orders");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");
        modelBuilder
            .Entity<Order>()
            .HasIndex(x => x.OrderNumber).IsUnique();
    }
}
