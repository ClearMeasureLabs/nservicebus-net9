using Microsoft.Data.SqlClient;
using NServiceBus.Transport.SqlServer;
using Shared;

namespace Orders.Extensions;

public static class NServiceBusExtensions
{
    public static void UseNServiceBus(this IHostApplicationBuilder builder, string endpointName, string schema = "dbo", bool isSendOnly = false)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);

        if (isSendOnly)
        {
            endpointConfiguration.SendOnly();
        }

        endpointConfiguration.EnableInstallers();
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.EnableOutbox();
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        // Configure Transport
        var transportConnectionString = builder.Configuration.GetConnectionString("Transport");
        SqlHelper.EnsureDatabaseExists(transportConnectionString);
        SqlHelper.CreateSchema(transportConnectionString, schema);

        var transport = new SqlServerTransport(transportConnectionString)
        {
            DefaultSchema = schema,
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
        SqlHelper.CreateSchema(persistenceConnectionString, schema);

        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(() => new SqlConnection(persistenceConnectionString));

        var dialect = persistence.SqlDialect<SqlDialect.MsSqlServer>();
        dialect.Schema(schema);
        persistence.TablePrefix("Nsb_");

        endpointConfiguration.Recoverability()
            .Immediate(s => s.NumberOfRetries(0))
            .Delayed(s => s.NumberOfRetries(2).TimeIncrease(TimeSpan.FromSeconds(5)));

        builder.UseNServiceBus(endpointConfiguration);
    }
}
