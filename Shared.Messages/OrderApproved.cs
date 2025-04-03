namespace Shared.Messages;

public class OrderApproved : IEvent
{
    public required string OrderNumber { get; set; }
}