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
        private readonly ILogger<OrderMcpTool> _logger;
        public OrderMcpTool(OrderLookupTool orderLookupTool, ILogger<OrderMcpTool> logger)
        {
            _orderLookupTool = orderLookupTool;
            _logger = logger;
        }

        [McpServerTool(Name = "get_order")]
        [Description("Look up an order by its order ID and return its details.")]
        public async Task<string> GetOrderAsync([Description("The order ID, such as O-2001.")] string orderId, 
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("MCP tool {ToolName} invoked", "get_order");
            var order = await _orderLookupTool.ExecuteAsync(orderId, cancellationToken);
            _logger.LogInformation("MCP tool {ToolName} completed. Found: {Found}", "get_order", order is not null);
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
