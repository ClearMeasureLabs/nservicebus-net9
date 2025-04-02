using Carter;
using Messages;
using Microsoft.EntityFrameworkCore;
using Orders.API.Database;

namespace Orders.API.Orders;

public static class ApproveOrder
{
    public class Request
    {
        public required string ApprovedBy { get; set; }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app) =>
            app.MapPost("/orders/{orderNumber}/approve", Handle);

        private static async Task<IResult> Handle(string orderNumber, Request request, AppDbContext dbContext, IMessageSession messageSession)
        {
            if (!await dbContext.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
            {
                return Results.NotFound();
            }

            var command = new Command
            {
                OrderNumber = orderNumber,
                ApprovedBy = request.ApprovedBy
            };

            await messageSession.SendLocal(command);

            return Results.AcceptedAtRoute(
                GetOrderByOrderNumber.Endpoint.Name,
                new { orderNumber },
                new { message = "Order approval received and is being processed." });
        }
    }

    public class Command
    {
        public required string OrderNumber { get; set; }
        public required string ApprovedBy { get; set; }
    }

    public class Handler : IHandleMessages<Command>
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<Command> _logger;

        public Handler(AppDbContext dbContext, ILogger<Command> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(Command message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Creating Order {OrderNumber} in database with Status = Submitted.", message.OrderNumber);

            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == message.OrderNumber, context.CancellationToken);

            if (order is null)
            {
                throw new Exception($"Order {message.OrderNumber} not found.");
            }

            if (order.Status == "Approved")
            {
                return;
            }

            if (order.Status != "Submitted")
            {
                throw new Exception($"Order {message.OrderNumber} is not in Submitted status.");
            }

            order.Status = "Approved";

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new OrderApproved
            {
                OrderNumber = message.OrderNumber
            });
        }
    }
}
