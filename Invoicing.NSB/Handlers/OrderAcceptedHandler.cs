using Messages;
using Microsoft.Extensions.Logging;
using NServiceBus;
using System.Threading.Tasks;

namespace Invoicing.Handlers;

public class OrderAcceptedHandler : IHandleMessages<OrderApproved>
{
    private readonly ILogger<OrderAcceptedHandler> logger;

    public OrderAcceptedHandler(ILogger<OrderAcceptedHandler> logger)
    {
        this.logger = logger;
    }

    public Task Handle(OrderApproved message, IMessageHandlerContext context)
    {
        logger.LogInformation("Creating invoice for Accepted Order {OrderNumber}.", message.OrderNumber);

        return Task.CompletedTask;
    }
}