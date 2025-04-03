using Carter;
using Microsoft.EntityFrameworkCore;
using Orders.Database;
using Orders.Entities;
using Shared.Messages;

namespace Orders.Orders;

public static class SubmitOrder
{
    public class ApiRequest
    {
        public required string OrderNumber { get; set; }
        public required string ProductCode { get; set; }
        public required int Quantity { get; set; }
        public required string VendorName { set; get; }
    }
    public class ApiResponse
    {
        public required string Message { get; set; }
        public required string OrderNumber { get; set; }
    }

    public class ApiEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app) =>
            app.MapPost("/orders", Handle);

        private static async Task<IResult> Handle(ApiRequest apiRequest, AppDbContext dbContext, IMessageSession messageSession, ILogger<ApiEndpoint> logger)
        {
            logger.LogInformation("API request received to submit order {OrderNumber}.", apiRequest.OrderNumber);

            if (await dbContext.Orders.AnyAsync(o => o.OrderNumber == apiRequest.OrderNumber))
            {
                logger.LogWarning("Rejected duplicate request for order {OrderNumber}", apiRequest.OrderNumber);
                return Results.Conflict();
            }

            var message = new Command
            {
                OrderNumber = apiRequest.OrderNumber,
                ProductCode = apiRequest.ProductCode,
                Quantity = apiRequest.Quantity,
                VendorName = apiRequest.VendorName
            };

            logger.LogInformation("Sending command to submit order {OrderNumber}.", apiRequest.OrderNumber);
            await messageSession.SendLocal(message);

            return Results.AcceptedAtRoute(
                GetOrderByOrderNumber.ApiEndpoint.Name,
                new { orderNumber = apiRequest.OrderNumber },
                new ApiResponse
                {
                    Message = "Order received and is being processed.",
                    OrderNumber = apiRequest.OrderNumber
                });
        }
    }

    public class Command
    {
        public required string OrderNumber { get; set; }
        public required string ProductCode { get; set; }
        public required int Quantity { get; set; }
        public required string VendorName { set; get; }
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
            _logger.LogInformation("Handling command to submit order {OrderNumber}.", message.OrderNumber);
            if (context.MessageHeaders.TryGetValue(Headers.DelayedRetries, out var delayedRetries))
            {
                var immediateRetries = context.MessageHeaders[Headers.ImmediateRetries];

                _logger.LogWarning("Delayed retries: {DelayedRetries}. Immediate retries: {ImmediateRetries}.",
                    delayedRetries, immediateRetries);
            }
            else
            {
                // Not in a retry so simulate some initial processing delay
                // e.g. Transport congestion, SQL Server latency, etc.
                await Task.Delay(TimeSpan.FromSeconds(5), context.CancellationToken);
            }

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
