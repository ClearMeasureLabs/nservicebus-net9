using Carter;
using Microsoft.EntityFrameworkCore;
using Orders.API.Database;
using Orders.API.Extensions;

Console.Title = AppDomain.CurrentDomain.FriendlyName;
Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started.");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSeq());

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Persistence")));

builder.UseNServiceBus("NServiceBusDemo.Orders.API", "orders");
builder.Services.AddCarter();

var app = builder.Build();

app.MapCarter();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    await app.MigrateDatabase();
}

await app.RunAsync();
