using System.ComponentModel.DataAnnotations;

namespace Orders.API.Entities;

public class Order
{
    [Key]
    public required Guid Id { get; set; }
    public required string OrderNumber { get; set; }
    public required string ProductCode { get; set; }
    public int Quantity { get; set; }
    public required string VendorName { get; set; }
    public required string Status { get; set; }
}
