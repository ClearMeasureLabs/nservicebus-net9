using API.Model;
using API.Services;
using Microsoft.Data.SqlClient;
using NServiceBus.Transport.SqlServer;
using Shared;

Console.Title = AppDomain.CurrentDomain.FriendlyName;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSeq());
builder.Services.AddTransient<IOrderService, OrderService>();

#region NService Configuration

var transportConnectionString = builder.Configuration.GetConnectionString("Transport");
SqlHelper.EnsureDatabaseExists(transportConnectionString);
SqlHelper.CreateSchema(transportConnectionString, Endpoints.OrdersApi.Schema);

var persistenceConnectionString = builder.Configuration.GetConnectionString("Persistence");
SqlHelper.EnsureDatabaseExists(persistenceConnectionString);
SqlHelper.CreateSchema(persistenceConnectionString, Endpoints.OrdersApi.Schema);

var endpointConfiguration = new EndpointConfiguration(Endpoints.OrdersApi.EndpointName);
endpointConfiguration.SendOnly();
endpointConfiguration.EnableInstallers();
endpointConfiguration.SendFailedMessagesTo("error");

var transport = new SqlServerTransport(transportConnectionString)
{
    DefaultSchema = Endpoints.OrdersApi.Schema,
    TransportTransactionMode = TransportTransactionMode.ReceiveOnly
};
transport.SchemaAndCatalog.UseSchemaForQueue("error", "dbo");
transport.SchemaAndCatalog.UseSchemaForQueue("audit", "dbo");

endpointConfiguration.UseTransport(transport);

var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
persistence.ConnectionBuilder(() => new SqlConnection(persistenceConnectionString));

var dialect = persistence.SqlDialect<SqlDialect.MsSqlServer>();
dialect.Schema(Endpoints.OrdersApi.Schema);
persistence.TablePrefix("");

transport.Subscriptions.DisableCaching = true;
transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName(
    table: "Subscriptions",
    schema: "dbo");

endpointConfiguration.EnableOutbox();

endpointConfiguration.UseSerialization<SystemJsonSerializer>();

builder.UseNServiceBus(endpointConfiguration);

#endregion

var app = builder.Build();

app.MapGet("/orders/{orderNumber:alpha}",
    (string orderNumber) => $"Details for order {orderNumber}");

app.MapPost("/orders",
    (SubmitOrderRequest request, IOrderService orderService) => orderService.SubmitOrder(request));

app.MapPost("/orders/{orderNumber:alpha}/accept",
    (string orderNumber, IOrderService orderService) => orderService.AcceptOrder(orderNumber));

app.Run();
