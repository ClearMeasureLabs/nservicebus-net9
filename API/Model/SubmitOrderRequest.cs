namespace API.Model;

internal class SubmitOrderRequest
{
    public required string OrderNumber { get; set; }
    public required string ProductCode { get; set; }
    public required int Quantity { get; set; }
    public required string VendorName { set; get; }
}