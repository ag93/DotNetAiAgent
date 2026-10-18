using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Support.Operations.Tools;
using System.Diagnostics;

namespace Support.Mcp.Server.Tools;

[McpServerToolType]
public sealed class CustomerMcpTool
{
    private readonly CustomerLookupTool _customerLookupTool;
    private readonly ILogger<CustomerMcpTool> _logger;

    public CustomerMcpTool(CustomerLookupTool customerLookupTool, ILogger<CustomerMcpTool> logger)
    {
        _customerLookupTool = customerLookupTool;
        _logger = logger;
    }

    [McpServerTool(Name = "get_customer")]
    [Description("Look up a customer using their customer ID.")]
    public async Task<string> GetCustomerAsync([Description("The customer ID, such as C-1002.")] string customerId,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("MCP tool {ToolName} invoked", "get_customer");

        try
        {
            var customer = await _customerLookupTool.ExecuteAsync(customerId, cancellationToken);

            _logger.LogInformation(
                "MCP tool {ToolName} completed. Found: {Found}. " +
                "DurationMs: {DurationMs}",
                "get_customer",
            customer is not null,
            stopwatch.ElapsedMilliseconds);

            if (customer is null)
            {
                return JsonSerializer.Serialize(new
                {
                    found = false,
                    customerId
                });
            }

            return JsonSerializer.Serialize(new
            {
                found = true,
                customer
            });
        }
        catch (OperationCanceledException)
        when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("MCP tool {ToolName} cancelled", "get_customer");
            throw;
        }
        catch (ArgumentException)
        {
            _logger.LogWarning("MCP tool {ToolName} received invalid arguments", "get_customer");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                "MCP tool {ToolName} failed due to a downstream HTTP error. " +
                "StatusCode: {StatusCode}",
                "get_customer",
                ex.StatusCode);
            throw;
        }
    }
}
