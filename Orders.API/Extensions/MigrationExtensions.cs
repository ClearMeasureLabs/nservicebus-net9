using Microsoft.EntityFrameworkCore;
using Orders.API.Database;

namespace Orders.API.Extensions;

public static class MigrationExtensions
{
    public static async Task MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
}
