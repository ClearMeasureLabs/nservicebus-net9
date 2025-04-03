using Carter;
using Microsoft.EntityFrameworkCore;
using NServiceBus.Persistence.Sql;
using Orders.Database;
using Orders.Extensions;

Console.Title = AppDomain.CurrentDomain.FriendlyName;
Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started.");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSeq());

builder.UseNServiceBus("NServiceBusDemo.Orders.API", "orders");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Persistence")));

builder.Services.AddScoped(services =>
{
    var session = services.GetService<ISqlStorageSession>();

    if (session is ISqlStorageSession { Connection: not null })
    {
        // If DbContext is being resolved from NServiceBus, use the same connection and transaction
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(session.Connection)
            .Options;

        var dbContext = new AppDbContext(options);

        dbContext.Database.UseTransaction(session.Transaction);

        // Ensure context is flushed before the transaction is committed
        session.OnSaveChanges((_, cancellationToken) => dbContext.SaveChangesAsync(cancellationToken));

        return dbContext;
    }
    else
    {
        // Otherwise DbContext is being resolved from an ASP.NET request
        var configuration = services.GetRequiredService<IConfiguration>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Persistence"))
            .Options;

        return new AppDbContext(options);
    }
});

builder.Services.AddCarter();

var app = builder.Build();

app.MapCarter();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    await app.MigrateDatabase();
}

await app.RunAsync();
