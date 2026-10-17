using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Support.Operations.Tools;

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
        _logger.LogInformation("MCP tool {ToolName} invoked", "get_customer");
        var customer = await _customerLookupTool.ExecuteAsync(customerId, cancellationToken);

        _logger.LogInformation("MCP tool {ToolName} completed. Found: {Found}", "get_customer", customer is not null);

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
}
