using API.Model;
using Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NServiceBus.Transport.SqlServer;
using Shared;

var builder = WebApplication.CreateBuilder(args);

#region NService Configuration

var endpointConfiguration = new EndpointConfiguration(Endpoints.OrdersApi.EndpointName);
endpointConfiguration.SendOnly();
endpointConfiguration.EnableInstallers();
endpointConfiguration.SendFailedMessagesTo("error");

const string connectionString = @"Data Source=(localdb)\mssqllocaldb;Database=NsbSamplesSqlOutbox;Trusted_Connection=True;MultipleActiveResultSets=true";

var transport = new SqlServerTransport(connectionString)
{
    DefaultSchema = Endpoints.OrdersApi.Schema,
    TransportTransactionMode = TransportTransactionMode.ReceiveOnly
};
transport.SchemaAndCatalog.UseSchemaForQueue("error", "dbo");
transport.SchemaAndCatalog.UseSchemaForQueue("audit", "dbo");

endpointConfiguration.UseTransport(transport);

var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
persistence.ConnectionBuilder(connectionBuilder: () => new SqlConnection(connectionString));

var dialect = persistence.SqlDialect<SqlDialect.MsSqlServer>();
dialect.Schema(Endpoints.OrdersApi.Schema);
persistence.TablePrefix("");

transport.Subscriptions.DisableCaching = true;
transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName(
    table: "Subscriptions",
    schema: "dbo");

endpointConfiguration.EnableOutbox();

endpointConfiguration.UseSerialization<SystemJsonSerializer>();

SqlHelper.CreateSchema(connectionString, Endpoints.OrdersApi.Schema);

builder.UseNServiceBus(endpointConfiguration);

#endregion

var app = builder.Build();

app.MapPost("/orders", async (SubmitOrderRequest request, [FromServices] IMessageSession messageSession) =>
{
    var message = new OrderSubmitted
    {
        OrderNumber = request.OrderNumber,
        ProductCode = request.ProductCode,
        Quantity = request.Quantity,
        VendorName = request.VendorName
    };

    await messageSession.Publish(message);

    return Results.AcceptedAtRoute($"/orders/{message.OrderNumber}");
});

app.MapGet("/orders/{orderNumber:alpha}", (string orderNumber) => $"Details for order {orderNumber}");

app.MapPost("/orders/{orderNumber:alpha}/accept", async (string orderNumber, [FromServices] IMessageSession messageSession) =>
{
    var message = new OrderAccepted
    {
        OrderNumber = orderNumber
    };

    await messageSession.Publish(message);

    return Results.AcceptedAtRoute($"/orders/{message.OrderNumber}");
});

app.Run();