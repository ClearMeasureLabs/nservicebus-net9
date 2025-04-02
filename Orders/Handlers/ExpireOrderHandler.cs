using Messages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Orders.Messages;
using System.Threading.Tasks;

namespace Orders.Handlers;

public class ExpireOrderHandler : IHandleMessages<ExpireOrder>
{
    private readonly ILogger<ExpireOrderHandler> _logger;

    public ExpireOrderHandler(ILogger<ExpireOrderHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ExpireOrder message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Updating Order {OrderNumber} in database with Status = Expired.", message.OrderNumber);

        var session = context.SynchronizedStorageSession.SqlPersistenceSession();

        const string sql = """
                           update [orders].[Order]
                           set Status = 'Expired'
                           where OrderNumber = @OrderNumber
                           """;

        await using var command = new SqlCommand(
            cmdText: sql,
            connection: (SqlConnection)session.Connection,
            transaction: (SqlTransaction)session.Transaction);

        var parameters = command.Parameters;
        parameters.AddWithValue("OrderNumber", message.OrderNumber);

        await command.ExecuteNonQueryAsync(context.CancellationToken);

        _logger.LogInformation("Publishing OrderExpired event for Order {OrderNumber}.", message.OrderNumber);

        await context.Publish(new OrderExpired
        {
            OrderNumber = message.OrderNumber
        });
    }
}