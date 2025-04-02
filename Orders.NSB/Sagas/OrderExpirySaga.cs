using System;
using System.Threading.Tasks;
using Messages;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using Orders.Messages;

namespace Orders.Sagas;

public class OrderExpirySaga : SqlSaga<OrderExpirySaga.SagaData>,
    IAmStartedByMessages<OrderSubmitted>,
    IHandleMessages<OrderAccepted>,
    IHandleMessages<OrderExpired>,
    IHandleTimeouts<OrderExpirationTimeout>
{
    private const long EXPIRES_IN_SECONDS = 30;

    private readonly ILogger<OrderExpirySaga> _logger;

    public OrderExpirySaga(ILogger<OrderExpirySaga> logger)
    {
        this._logger = logger;
    }

    protected override void ConfigureMapping(IMessagePropertyMapper mapper)
    {
        mapper.ConfigureMapping<OrderSubmitted>(msg => msg.OrderNumber);
        mapper.ConfigureMapping<OrderAccepted>(msg => msg.OrderNumber);
        mapper.ConfigureMapping<OrderExpired>(msg => msg.OrderNumber);
    }

    protected override string CorrelationPropertyName => nameof(SagaData.OrderNumber);

    public Task Handle(OrderSubmitted message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Starting OrderExpirySaga for newly Submitted Order {OrderNumber}.", message.OrderNumber);

        return RequestTimeout(context, TimeSpan.FromSeconds(EXPIRES_IN_SECONDS), new OrderExpirationTimeout());
    }

    public async Task Timeout(OrderExpirationTimeout state, IMessageHandlerContext context)
    {
        _logger.LogInformation("Order expiration timeout elapsed for Order {OrderNumber}", Data.OrderNumber);

        var message = new ExpireOrder
        {
            OrderNumber = Data.OrderNumber
        };

        await context.SendLocal(message);
    }

    public Task Handle(OrderAccepted message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Order {OrderNumber} has been Accepted. Completing saga.", message.OrderNumber);

        MarkAsComplete();

        return Task.CompletedTask;
    }

    public Task Handle(OrderExpired message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Order {OrderNumber} has Expired. Completing saga.", message.OrderNumber);

        MarkAsComplete();

        return Task.CompletedTask;
    }

    public class SagaData :
        ContainSagaData
    {
        public string OrderNumber { get; set; }
    }

}
