using Carter;
using Microsoft.EntityFrameworkCore;
using Orders.Database;
using Shared.Messages;

namespace Orders.Orders;

public static class ApproveOrder
{
    public class ApiRequest
    {
        public required string ApprovedBy { get; set; }
    }

    public class ApiEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app) =>
            app.MapPost("/orders/{orderNumber}/approve", Handle);

        private static async Task<IResult> Handle(string orderNumber, ApiRequest apiRequest, AppDbContext dbContext, IMessageSession messageSession, ILogger<ApiEndpoint> logger)
        {
            logger.LogInformation("Order {OrderNumber} approval request received.", orderNumber);

            if (!await dbContext.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
            {
                return Results.NotFound();
            }

            var command = new Command
            {
                OrderNumber = orderNumber,
                ApprovedBy = apiRequest.ApprovedBy
            };

            await messageSession.SendLocal(command);

            return Results.AcceptedAtRoute(
                GetOrderByOrderNumber.ApiEndpoint.Name,
                new { orderNumber },
                new { message = "Order approval received and is being processed." });
        }
    }

    public class Command
    {
        public required string OrderNumber { get; set; }
        public required string ApprovedBy { get; set; }
    }

    public class CommandHandler : IHandleMessages<Command>
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<Command> _logger;

        public CommandHandler(AppDbContext dbContext, ILogger<Command> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(Command message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Approving Order {OrderNumber}.", message.OrderNumber);

            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == message.OrderNumber, context.CancellationToken);

            if (order is null)
            {
                throw new Exception($"Order {message.OrderNumber} not found.");
            }

            switch (order.Status)
            {
                case "Approved":
                    _logger.LogInformation("Order {OrderNumber} is already approved", order.OrderNumber);
                    return;
                case "Expired":
                    _logger.LogWarning("Order {OrderNumber} has expired and cannot be approved", order.OrderNumber);
                    return;
                case "Submitted":
                    order.Status = "Approved";
                    await _dbContext.SaveChangesAsync(context.CancellationToken);
                    await context.Publish(new OrderApproved
                    {
                        OrderNumber = message.OrderNumber
                    });
                    return;
            }
        }
    }
}
