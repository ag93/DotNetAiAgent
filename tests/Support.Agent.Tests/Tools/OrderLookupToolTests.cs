using System.Net;
using Support.Operations.Services;
using Support.Operations.Tools;
using Support.Agent.Tests.TestHelpers;

namespace Support.Agent.Tests.Tools;

public class OrderLookupToolTests
{
    [Fact]
    public async Task ExecuteAsync_WhenOrderIdIsMissing_ThrowsArgumentException()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var orderClient = new BusinessApiOrderClient(httpClient);
        var tool = new OrderLookupTool(orderClient);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync("   "));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderExists_ReturnsOrder()
    {
        // Arrange
        const string responseBody = """
        {
            "id": "O-2001",
            "customerId": "C-1002",
            "status": "Shipped",
            "totalAmount": 129.99,
            "placedAt": "2026-10-10T14:30:00+00:00"
        }
        """;

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var orderClient = new BusinessApiOrderClient(httpClient);
        var tool = new OrderLookupTool(orderClient);

        // Act
        var order = await tool.ExecuteAsync("O-2001");

        // Assert
        Assert.NotNull(order);
        Assert.Equal("O-2001", order.Id);
        Assert.Equal("C-1002", order.CustomerId);
        Assert.Equal("Shipped", order.Status);
        Assert.Equal(129.99m, order.TotalAmount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var orderClient = new BusinessApiOrderClient(httpClient);
        var tool = new OrderLookupTool(orderClient);

        // Act
        var order = await tool.ExecuteAsync("UNKNOWN");

        // Assert
        Assert.Null(order);
    }
}
