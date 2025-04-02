using Carter;
using Microsoft.EntityFrameworkCore;
using Orders.API.Database;

namespace Orders.API.Orders;

public static class GetOrderByOrderNumber
{
    public class Response
    {
        public required string OrderNumber { get; set; }
        public required string ProductCode { get; set; }
        public required int Quantity { get; set; }
        public required string Status { get; set; }
        public required string VendorName { get; set; }
    }

    public class Endpoint : ICarterModule
    {
        public const string Name = nameof(GetOrderByOrderNumber);

        public void AddRoutes(IEndpointRouteBuilder app) => app
            .MapGet("/orders/{orderNumber}", Handle)
            .WithName(Name);

        private static async Task<IResult> Handle(string orderNumber, AppDbContext dbContext)
        {
            var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
            return order is null
                ? Results.NotFound()
                : Results.Ok(new Response
                {
                    OrderNumber = order.OrderNumber,
                    ProductCode = order.ProductCode,
                    Quantity = order.Quantity,
                    Status = order.Status,
                    VendorName = order.VendorName
                });
        }
    }
}
