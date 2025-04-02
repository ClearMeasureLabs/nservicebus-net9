using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Transport.SqlServer;
using Shared;

namespace Orders;

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
                SqlHelper.ExecuteSql(hostBuilderContext.Configuration.GetConnectionString("Persistence"), File.ReadAllText("Migrations.sql"));
            })
            .UseNServiceBus(hostBuilderContext =>
            {
                var endpointConfiguration = new EndpointConfiguration(Endpoints.Orders.Name);
                endpointConfiguration.EnableInstallers();
                endpointConfiguration.SendFailedMessagesTo("error");
                endpointConfiguration.EnableOutbox();
                endpointConfiguration.UseSerialization<SystemJsonSerializer>();

                // Configure Transport
                var transportConnectionString = hostBuilderContext.Configuration.GetConnectionString("Transport");
                SqlHelper.EnsureDatabaseExists(transportConnectionString);
                SqlHelper.CreateSchema(transportConnectionString, Endpoints.Orders.Schema);

                var transport = new SqlServerTransport(transportConnectionString)
                {
                    DefaultSchema = Endpoints.Orders.Schema,
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
                SqlHelper.CreateSchema(persistenceConnectionString, Endpoints.Orders.Schema);

                var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
                persistence.ConnectionBuilder(() => new SqlConnection(persistenceConnectionString));

                var dialect = persistence.SqlDialect<SqlDialect.MsSqlServer>();
                dialect.Schema(Endpoints.Orders.Schema);
                persistence.TablePrefix("");

                return endpointConfiguration;
            });


}
