namespace Messages;

public class OrderSubmitted : IEvent
{
    public required string OrderNumber { get; set; }
}
