using API.Model;
using Messages;

namespace API.Services;

public interface IOrderService
{
    public Task<IResult> SubmitOrder(SubmitOrderRequest request);
    public Task<IResult> AcceptOrder(string orderNumber);
}

public class OrderService : IOrderService
{
    private readonly IMessageSession _messageSession;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IMessageSession messageSession, ILogger<OrderService> logger)
    {
        _messageSession = messageSession;
        _logger = logger;
    }

    public async Task<IResult> SubmitOrder(SubmitOrderRequest request)
    {
        _logger.LogInformation("Submitting new order {OrderNumber}", request.OrderNumber);

        var message = new OrderSubmitted
        {
            OrderNumber = request.OrderNumber,
            ProductCode = request.ProductCode,
            Quantity = request.Quantity,
            VendorName = request.VendorName
        };

        await _messageSession.Publish(message);

        return Results.Accepted();
    }

    public async Task<IResult> AcceptOrder(string orderNumber)
    {
        _logger.LogInformation("Approving order {OrderNumber}", orderNumber);

        var message = new OrderAccepted
        {
            OrderNumber = orderNumber
        };

        await _messageSession.Publish(message);

        return Results.Accepted();
    }
}
