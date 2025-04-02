using NServiceBus;

namespace Orders.Messages;

public class ExpireOrder : ICommand
{
    public string OrderNumber { get; set; }
}