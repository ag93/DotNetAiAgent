using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Support.Operations.Tools;

namespace Support.Mcp.Server.Tools;

[McpServerToolType]
public sealed class CustomerMcpTool
{
    private readonly CustomerLookupTool _customerLookupTool;

    public CustomerMcpTool(CustomerLookupTool customerLookupTool)
    {
        _customerLookupTool = customerLookupTool;
    }

    [McpServerTool(Name = "get_customer")]
    [Description("Look up a customer using their customer ID.")]
    public async Task<string> GetCustomerAsync([Description("The customer ID, such as C-1002.")] string customerId,
        CancellationToken cancellationToken)
    {
        var customer = await _customerLookupTool.ExecuteAsync(customerId, cancellationToken);

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
