namespace Messages;

public class OrderExpired : IEvent
{
    public required string OrderNumber { get; set; }
}