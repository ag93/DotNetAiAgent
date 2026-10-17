using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Support.Operations.Tools;

namespace Support.Mcp.Server.Tools
{
    [McpServerToolType]
    public sealed class OrderMcpTool
    {
        private readonly OrderLookupTool _orderLookupTool;
        public OrderMcpTool(OrderLookupTool orderLookupTool)
        {
            _orderLookupTool = orderLookupTool;
        }

        [McpServerTool(Name = "get_order")]
        [Description("Look up an order by its order ID and return its details.")]
        public async Task<string> GetOrderAsync([Description("The order ID, such as O-2001.")] string orderId, 
            CancellationToken cancellationToken)
        {
            var order = await _orderLookupTool.ExecuteAsync(orderId, cancellationToken);
            if (order is null)
            {
                return JsonSerializer.Serialize(new
                {
                    found = false,
                    orderId
                });
            }

            return JsonSerializer.Serialize(new
            {
                found = true,
                order
            });
        }
    }
}
