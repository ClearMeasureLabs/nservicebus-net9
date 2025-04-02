using API.Model;
using API.Services;
using Microsoft.Data.SqlClient;
using NServiceBus.Transport.SqlServer;
using Shared;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.Title = AppDomain.CurrentDomain.FriendlyName;

        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder);
        ConfigureNServiceBus(builder);

        var app = builder.Build();
        ConfigureRouting(app);

        app.Run();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSeq());
        builder.Services.AddTransient<IOrderService, OrderService>();
    }

    private static void ConfigureNServiceBus(WebApplicationBuilder builder)
    {
        var thisEndpoint = Endpoints.OrdersApi;
        var endpointConfiguration = new EndpointConfiguration(thisEndpoint.Name);

        endpointConfiguration.SendOnly();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.EnableOutbox();
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        // Configure Transport
        var transportConnectionString = builder.Configuration.GetConnectionString("Transport");
        SqlHelper.EnsureDatabaseExists(transportConnectionString);
        SqlHelper.CreateSchema(transportConnectionString, thisEndpoint.Schema);

        var transport = new SqlServerTransport(transportConnectionString)
        {
            DefaultSchema = thisEndpoint.Schema,
            TransportTransactionMode = TransportTransactionMode.ReceiveOnly
        };

        transport.SchemaAndCatalog.UseSchemaForQueue("error", "dbo");
        transport.SchemaAndCatalog.UseSchemaForQueue("audit", "dbo");

        transport.Subscriptions.DisableCaching = true;
        transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName(
            table: "Subscriptions",
            schema: "dbo");

        endpointConfiguration.UseTransport(transport);

        // Configure Persistence
        var persistenceConnectionString = builder.Configuration.GetConnectionString("Persistence");
        SqlHelper.EnsureDatabaseExists(persistenceConnectionString);
        SqlHelper.CreateSchema(persistenceConnectionString, thisEndpoint.Schema);

        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(() => new SqlConnection(persistenceConnectionString));

        var dialect = persistence.SqlDialect<SqlDialect.MsSqlServer>();
        dialect.Schema(thisEndpoint.Schema);
        persistence.TablePrefix("");

        builder.UseNServiceBus(endpointConfiguration);
    }

    private static void ConfigureRouting(WebApplication app)
    {
        app.MapGet("/orders/{orderNumber:alpha}",
            (string orderNumber) => $"Details for order {orderNumber}");

        app.MapPost("/orders",
            (SubmitOrderRequest request, IOrderService orderService) => orderService.SubmitOrder(request));

        app.MapPost("/orders/{orderNumber:alpha}/accept",
            (string orderNumber, IOrderService orderService) => orderService.AcceptOrder(orderNumber));
    }
}
