using System;
using System.Threading.Tasks;
using Messages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Orders.Handlers;

public class OrderSubmittedHandler : IHandleMessages<OrderSubmitted>
{
    private readonly ILogger<OrderSubmittedHandler> _logger;

    public OrderSubmittedHandler(ILogger<OrderSubmittedHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderSubmitted message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Creating Order {OrderNumber} in database with Status = Submitted.", message.OrderNumber);

        var session = context.SynchronizedStorageSession.SqlPersistenceSession();

        const string sql = """
                           insert into [orders].[Order] (Id, OrderNumber, ProductCode, Quantity, VendorName, Status)
                           values (@Id, @OrderNumber, @ProductCode, @Quantity, @VendorName, @Status)
                           """;

        await using var command = new SqlCommand(
            cmdText: sql,
            connection: (SqlConnection)session.Connection,
            transaction: (SqlTransaction)session.Transaction);

        var parameters = command.Parameters;
        var newOrderId = Guid.NewGuid();
        parameters.AddWithValue("Id", newOrderId);
        parameters.AddWithValue("Status", "Submitted");
        parameters.AddWithValue("OrderNumber", message.OrderNumber);
        parameters.AddWithValue("ProductCode", message.ProductCode);
        parameters.AddWithValue("Quantity", message.Quantity);
        parameters.AddWithValue("VendorName", message.VendorName);

        await command.ExecuteNonQueryAsync(context.CancellationToken);
    }
}
