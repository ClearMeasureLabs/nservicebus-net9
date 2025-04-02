using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
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
            .UseNServiceBus(x =>
            {
                var endpointConfiguration = new EndpointConfiguration(Endpoints.Invoicing.EndpointName);
                endpointConfiguration.EnableInstallers();
                endpointConfiguration.SendFailedMessagesTo("error");

                const string connectionString = @"Data Source=(localdb)\mssqllocaldb;Database=NServiceBusDemo;Trusted_Connection=True;MultipleActiveResultSets=true";

                var transport = new SqlServerTransport(connectionString)
                {
                    DefaultSchema = Endpoints.Invoicing.Schema,
                    TransportTransactionMode = TransportTransactionMode.ReceiveOnly
                };
                transport.SchemaAndCatalog.UseSchemaForQueue("error", "dbo");
                transport.SchemaAndCatalog.UseSchemaForQueue("audit", "dbo");

                endpointConfiguration.UseTransport(transport);

                var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
                persistence.ConnectionBuilder(connectionBuilder: () => new SqlConnection(connectionString));

                var dialect = persistence.SqlDialect<SqlDialect.MsSqlServer>();
                dialect.Schema(Endpoints.Invoicing.Schema);
                persistence.TablePrefix("");

                transport.Subscriptions.DisableCaching = true;
                transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName(
                    table: "Subscriptions",
                    schema: "dbo");

                endpointConfiguration.EnableOutbox();

                endpointConfiguration.UseSerialization<SystemJsonSerializer>();

                SqlHelper.CreateSchema(connectionString, Endpoints.Invoicing.Schema);
                return endpointConfiguration;
            });
}
