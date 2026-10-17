using System.Net;
using Support.Agent.Api.Services;
using Support.Agent.Api.Tools;
using Support.Agent.Tests.TestHelpers;

namespace Support.Agent.Tests.Tools;

public class CustomerLookupToolTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCustomerIdIsMissing_ThrowsArgumentException()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var customerClient = new BusinessApiCustomerClient(httpClient);

        var tool = new CustomerLookupTool(customerClient);

        await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync("   "));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCustomerExists_ReturnsCustomer()
    {
        const string responseBody = """
        {
            "id": "C-1002",
            "name": "Alex Morgan",
            "email": "alex@example.com"
        }
        """;

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var customerClient = new BusinessApiCustomerClient(httpClient);
        var tool = new CustomerLookupTool(customerClient);

        var customer = await tool.ExecuteAsync("C-1002");

        Assert.NotNull(customer);
        Assert.Equal("C-1002", customer.Id);
        Assert.Equal("Alex Morgan", customer.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCustomerDoesNotExist_ReturnsNull()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var customerClient = new BusinessApiCustomerClient(httpClient);
        var tool = new CustomerLookupTool(customerClient);

        var customer = await tool.ExecuteAsync("UNKNOWN");

        Assert.Null(customer);
    }
}
