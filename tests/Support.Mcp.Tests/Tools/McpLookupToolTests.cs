using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Support.Mcp.Server.Tools;
using Support.Mcp.Tests.TestHelpers;
using Support.Operations.Services;
using Support.Operations.Tools;

namespace Support.Mcp.Tests.Tools;

public class McpLookupToolTests
{
    [Fact]
    public async Task GetCustomer_WhenFound_ReturnsCustomer()
    {
        const string customerJson = """
        {
            "id": "C-1002",
            "name": "Alex Morgan",
            "email": "alex@example.com"
        }
        """;

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, customerJson);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var lookupTool = new CustomerLookupTool(new BusinessApiCustomerClient(httpClient));

        var mcpTool = new CustomerMcpTool(lookupTool, NullLogger<CustomerMcpTool>.Instance);

        var result = await mcpTool.GetCustomerAsync("C-1002", CancellationToken.None);

        using var json = JsonDocument.Parse(result);

        Assert.True(json.RootElement.GetProperty("found").GetBoolean());

        Assert.Equal("/api/customers/C-1002", handler.RequestedPath);
    }

    [Fact]
    public async Task GetCustomer_WhenMissing_ReturnsNotFound()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var lookupTool = new CustomerLookupTool(new BusinessApiCustomerClient(httpClient));

        var mcpTool = new CustomerMcpTool(lookupTool, NullLogger<CustomerMcpTool>.Instance);

        var result = await mcpTool.GetCustomerAsync("UNKNOWN", CancellationToken.None);

        using var json = JsonDocument.Parse(result);

        Assert.False(json.RootElement.GetProperty("found").GetBoolean());

        Assert.Equal("UNKNOWN", json.RootElement.GetProperty("customerId").GetString());
    }

    [Fact]
    public async Task GetOrder_WhenFound_ReturnsOrder()
    {
        const string orderJson = """
        {
            "id": "O-2001",
            "customerId": "C-1002",
            "status": "Shipped",
            "totalAmount": 129.99,
            "placedAt": "2026-10-10T14:30:00+00:00"
        }
        """;

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, orderJson);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var lookupTool = new OrderLookupTool(new BusinessApiOrderClient(httpClient));

        var mcpTool = new OrderMcpTool(lookupTool, NullLogger<OrderMcpTool>.Instance);

        var result = await mcpTool.GetOrderAsync("O-2001", CancellationToken.None);

        using var json = JsonDocument.Parse(result);

        Assert.True(json.RootElement.GetProperty("found").GetBoolean());

        Assert.Equal("/api/orders/O-2001", handler.RequestedPath);
    }
}
