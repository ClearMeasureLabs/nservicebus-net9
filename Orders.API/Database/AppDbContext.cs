using Microsoft.EntityFrameworkCore;
using Orders.API.Entities;

namespace Orders.API.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
}
