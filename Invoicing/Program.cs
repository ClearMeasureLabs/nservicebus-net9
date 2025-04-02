using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Transport.SqlServer;
using Shared;

namespace Invoicing;

internal class Program
{
    public static async Task Main(string[] args)
    {
        Console.Title = AppDomain.CurrentDomain.FriendlyName;

        await CreateHostBuilder(args).Build().RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostBuilderContext, services) =>
            {
                services.AddLogging(loggingBuilder => loggingBuilder.AddSeq());
            })
            .UseNServiceBus(hostBuilderContext =>
            {
                var thisEndpoint = Endpoints.Invoicing;
                var endpointConfiguration = new EndpointConfiguration(thisEndpoint.Name);
                endpointConfiguration.EnableInstallers();
                endpointConfiguration.SendFailedMessagesTo("error");
                endpointConfiguration.EnableOutbox();
                endpointConfiguration.UseSerialization<SystemJsonSerializer>();

                // Configure Transport
                var transportConnectionString = hostBuilderContext.Configuration.GetConnectionString("Transport");
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
                var persistenceConnectionString = hostBuilderContext.Configuration.GetConnectionString("Persistence");
                SqlHelper.EnsureDatabaseExists(persistenceConnectionString);
                SqlHelper.CreateSchema(persistenceConnectionString, thisEndpoint.Schema);

                var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
                persistence.ConnectionBuilder(() => new SqlConnection(persistenceConnectionString));

                var dialect = persistence.SqlDialect<SqlDialect.MsSqlServer>();
                dialect.Schema(thisEndpoint.Schema);
                persistence.TablePrefix("");

                return endpointConfiguration;
            });
}
