using Microsoft.EntityFrameworkCore;

namespace Orders.Database;

public static class MigrationExtensions
{
    public static async Task MigrateDatabase(this WebApplication app)
    {
        var dbContextFactory = new MigrationDbContextFactory();
        var dbContext = dbContextFactory.CreateDbContext([]);
        await dbContext.Database.MigrateAsync();
    }
}
