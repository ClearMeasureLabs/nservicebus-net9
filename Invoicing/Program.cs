using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.Transport.SqlServer;
using Shared;
using System;
using System.Threading.Tasks;

namespace Invoicing;

class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
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
                Console.WriteLine("Press enter to send a message");
                return endpointConfiguration;
            });
}