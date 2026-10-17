using Support.Contracts.Orders;

namespace Support.Business.Api.Services;

public sealed class OrderService
{
    private readonly Dictionary<string, OrderResponse> _orders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["O-2001"] = new("O-2001", "C-1002", "Shipped", 129.99m,
                new DateTimeOffset(2026, 10, 10, 14, 30, 0, TimeSpan.Zero)),

            ["O-2002"] = new("O-2002", "C-1003", "Processing", 79.50m,
                new DateTimeOffset(2026, 10, 12, 9, 15, 0, TimeSpan.Zero))
        };

    public OrderResponse? GetById(string orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        return order;
    }
}
