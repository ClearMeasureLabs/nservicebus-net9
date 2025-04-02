namespace Messages;

public class OrderAccepted : IEvent
{
    public required string OrderNumber { get; set; }
}