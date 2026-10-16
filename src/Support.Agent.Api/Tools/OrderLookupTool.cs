using Support.Agent.Api.Services;
using Support.Contracts.Orders;

namespace Support.Agent.Api.Tools;

public sealed class OrderLookupTool
{
    private readonly BusinessApiOrderClient _orderClient;

    public OrderLookupTool(BusinessApiOrderClient orderClient)
    {
        _orderClient = orderClient;
    }

    public async Task<OrderResponse?> ExecuteAsync(string orderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        return await _orderClient.GetByIdAsync(orderId.Trim(), cancellationToken);
    }
}
