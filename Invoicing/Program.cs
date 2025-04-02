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
        await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostBuilderContext, services) =>
            {
                Console.Title = AppDomain.CurrentDomain.FriendlyName;

                services.AddLogging(loggingBuilder => loggingBuilder.AddSeq());
            })
            .UseNServiceBus(hostBuilderContext =>
            {
                var transportConnectionString = hostBuilderContext.Configuration.GetConnectionString("Transport");
                SqlHelper.EnsureDatabaseExists(transportConnectionString);
                SqlHelper.CreateSchema(transportConnectionString, Endpoints.Invoicing.Schema);

                var persistenceConnectionString = hostBuilderContext.Configuration.GetConnectionString("Persistence");
                SqlHelper.EnsureDatabaseExists(persistenceConnectionString);
                SqlHelper.CreateSchema(persistenceConnectionString, Endpoints.Invoicing.Schema);

                var endpointConfiguration = new EndpointConfiguration(Endpoints.Invoicing.EndpointName);
                endpointConfiguration.EnableInstallers();
                endpointConfiguration.SendFailedMessagesTo("error");

                var transport = new SqlServerTransport(transportConnectionString)
                {
                    DefaultSchema = Endpoints.Invoicing.Schema,
                    TransportTransactionMode = TransportTransactionMode.ReceiveOnly
                };
                transport.SchemaAndCatalog.UseSchemaForQueue("error", "dbo");
                transport.SchemaAndCatalog.UseSchemaForQueue("audit", "dbo");

                endpointConfiguration.UseTransport(transport);

                var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
                persistence.ConnectionBuilder(() => new SqlConnection(persistenceConnectionString));

                var dialect = persistence.SqlDialect<SqlDialect.MsSqlServer>();
                dialect.Schema(Endpoints.Invoicing.Schema);
                persistence.TablePrefix("");

                transport.Subscriptions.DisableCaching = true;
                transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName(
                    table: "Subscriptions",
                    schema: "dbo");

                endpointConfiguration.EnableOutbox();

                endpointConfiguration.UseSerialization<SystemJsonSerializer>();
                return endpointConfiguration;
            });
}
