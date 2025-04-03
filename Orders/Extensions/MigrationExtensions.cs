using Microsoft.EntityFrameworkCore;
using Orders.Database;

namespace Orders.Extensions;

public static class MigrationExtensions
{
    public static async Task MigrateDatabase(this WebApplication app)
    {
        var dbContextFactory = new MigrationsDbContextFactory();
        var dbContext = dbContextFactory.CreateDbContext([]);
        await dbContext.Database.MigrateAsync();
    }
}
