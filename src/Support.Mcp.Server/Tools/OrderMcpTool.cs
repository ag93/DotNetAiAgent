using ModelContextProtocol.Server;
using Support.Operations.Tools;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

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
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("MCP tool {ToolName} invoked", "get_order");
            try
            {
                var order = await _orderLookupTool.ExecuteAsync(orderId, cancellationToken);

                _logger.LogInformation(
                            "MCP tool {ToolName} completed. Found: {Found}. " +
                            "DurationMs: {DurationMs}",
                            "get_order",
                            order is not null,
                            stopwatch.ElapsedMilliseconds);

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
            catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("MCP tool {ToolName} cancelled", "get_order");
                throw;
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("MCP tool {ToolName} received invalid arguments", "get_order");
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    "MCP tool {ToolName} failed due to a downstream HTTP error. " +
                    "StatusCode: {StatusCode}",
                    "get_order",
                    ex.StatusCode);
                throw;
            }
        }
    }
}
