using Microsoft.EntityFrameworkCore;
using Orders.API.Database;

namespace Orders.API.Extensions;

public static class MigrationExtensions
{
    public static async Task MigrateDatabase(this WebApplication app)
    {
        var dbContextFactory = new MigrationsDbContextFactory();
        var dbContext = dbContextFactory.CreateDbContext([]);
        await dbContext.Database.MigrateAsync();
    }
}
