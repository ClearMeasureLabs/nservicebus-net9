using Carter;
using Messages;
using Microsoft.EntityFrameworkCore;
using Orders.API.Database;
using Orders.API.Entities;

namespace Orders.API.Orders;

public static class SubmitOrder
{
    public class Request
    {
        public required string OrderNumber { get; set; }
        public required string ProductCode { get; set; }
        public required int Quantity { get; set; }
        public required string VendorName { set; get; }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app) =>
            app.MapPost("/orders", Handle);

        private static async Task<IResult> Handle(Request request, AppDbContext dbContext, IMessageSession messageSession)
        {
            if (await dbContext.Orders.AnyAsync(o => o.OrderNumber == request.OrderNumber))
            {
                return Results.Conflict();
            }

            var message = new Command
            {
                OrderNumber = request.OrderNumber,
                ProductCode = request.ProductCode,
                Quantity = request.Quantity,
                VendorName = request.VendorName
            };

            await messageSession.SendLocal(message);

            return Results.AcceptedAtRoute(nameof(GetOrderByOrderNumber), new { orderNumber = request.OrderNumber }, new { message = "Order received and is being processed." });
        }
    }

    public class Command
    {
        public required string OrderNumber { get; set; }
        public required string ProductCode { get; set; }
        public required int Quantity { get; set; }
        public required string VendorName { set; get; }
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

            _dbContext.Orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = message.OrderNumber,
                ProductCode = message.ProductCode,
                Quantity = message.Quantity,
                VendorName = message.VendorName,
                Status = "Submitted"
            });

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new OrderSubmitted
            {
                OrderNumber = message.OrderNumber
            });
        }
    }
}
