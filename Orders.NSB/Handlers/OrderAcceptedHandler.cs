using Messages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NServiceBus;
using System.Threading.Tasks;

namespace Orders.Handlers;

public class OrderAcceptedHandler : IHandleMessages<OrderAccepted>
{
    private readonly ILogger<OrderAcceptedHandler> _logger;

    public OrderAcceptedHandler(ILogger<OrderAcceptedHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderAccepted message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Updating Order {OrderNumber} in database with Status = Accepted.", message.OrderNumber);

        var session = context.SynchronizedStorageSession.SqlPersistenceSession();

        const string sql = """
                           update [orders].[Order]
                           set Status = 'Accepted'
                           where OrderNumber = @OrderNumber
                           """;

        await using var command = new SqlCommand(
            cmdText: sql,
            connection: (SqlConnection)session.Connection,
            transaction: (SqlTransaction)session.Transaction);

        var parameters = command.Parameters;
        parameters.AddWithValue("OrderNumber", message.OrderNumber);

        await command.ExecuteNonQueryAsync(context.CancellationToken);
    }
}
