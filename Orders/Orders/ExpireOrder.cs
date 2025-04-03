using Microsoft.EntityFrameworkCore;
using Orders.Database;
using Shared.Messages;

namespace Orders.Orders;

public static class ExpireOrder
{
    public class Command : ICommand
    {
        public required string OrderNumber { get; set; }
    }

    public class CommandHandler : IHandleMessages<Command>
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<CommandHandler> _logger;

        public CommandHandler(AppDbContext dbContext, ILogger<CommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(Command message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Expiring Order {OrderNumber}.", message.OrderNumber);

            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == message.OrderNumber, context.CancellationToken);

            if (order is null)
            {
                throw new Exception($"Order {message.OrderNumber} not found.");
            }

            switch (order.Status)
            {
                case "Approved":
                    _logger.LogWarning("Order {OrderNumber} is approved and cannot be expired", order.OrderNumber);
                    return;
                case "Expired":
                    _logger.LogInformation("Order {OrderNumber} is already expired", order.OrderNumber);
                    return;
                case "Submitted":
                    order.Status = "Expired";
                    await _dbContext.SaveChangesAsync(context.CancellationToken);
                    await context.Publish(new OrderExpired
                    {
                        OrderNumber = message.OrderNumber
                    });
                    return;
            }
        }
    }
}
