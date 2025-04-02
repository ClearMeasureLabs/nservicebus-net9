using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.Transport.SqlServer;
using Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Orders;

class Program
{

    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                Console.Title = "Server";
            }).UseNServiceBus(x =>
            {
                Console.Title = Endpoints.Orders.EndpointName;

                const string connectionString = @"Data Source=(localdb)\mssqllocaldb;Database=NServiceBusDemo;Trusted_Connection=True;MultipleActiveResultSets=true";

                var endpointConfiguration = new EndpointConfiguration(Endpoints.Orders.EndpointName);
                endpointConfiguration.EnableInstallers();
                endpointConfiguration.SendFailedMessagesTo("error");

                var transport = new SqlServerTransport(connectionString)
                {
                    DefaultSchema = Endpoints.Orders.Schema,
                    TransportTransactionMode = TransportTransactionMode.ReceiveOnly
                };
                transport.SchemaAndCatalog.UseSchemaForQueue("error", "dbo");
                transport.SchemaAndCatalog.UseSchemaForQueue("audit", "dbo");

                var routing = endpointConfiguration.UseTransport(transport);
                routing.UseSchemaForEndpoint(Endpoints.OrdersApi.EndpointName, Endpoints.OrdersApi.Schema);

                var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
                persistence.ConnectionBuilder(() => new SqlConnection(connectionString));

                var dialect = persistence.SqlDialect<SqlDialect.MsSqlServer>();
                dialect.Schema(Endpoints.Orders.Schema);
                persistence.TablePrefix("");

                transport.Subscriptions.DisableCaching = true;
                transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName(
                    table: "Subscriptions",
                    schema: "dbo");

                endpointConfiguration.EnableOutbox();

                endpointConfiguration.UseSerialization<SystemJsonSerializer>();

                // Create application tables
                SqlHelper.CreateSchema(connectionString, Endpoints.Orders.Schema);
                SqlHelper.ExecuteSql(connectionString, File.ReadAllText("Startup.sql"));

                return endpointConfiguration;
            });


}