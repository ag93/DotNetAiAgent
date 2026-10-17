using System.Net;
using System.Text.Json;
using Support.Operations.Services;
using Support.Agent.Tests.TestHelpers;
using Support.Contracts.Orders;

namespace Support.Agent.Tests;

public class BusinessApiOrderClientTests
{
    [Fact]
    public async Task GetById_WhenOrderExists_ReturnsOrder()
    {
        // Arrange
        var expectedOrder = new OrderResponse("O-2001", "C-1002", "Shipped", 129.99m, 
            new DateTimeOffset(2026, 10, 10, 14, 30, 0, TimeSpan.Zero));

        var responseBody = JsonSerializer.Serialize(expectedOrder);

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var client = new BusinessApiOrderClient(httpClient);

        // Act
        var order = await client.GetByIdAsync("O-2001");

        // Assert
        Assert.NotNull(order);
        Assert.Equal("O-2001", order.Id);
        Assert.Equal("C-1002", order.CustomerId);
        Assert.Equal("Shipped", order.Status);
        Assert.Equal(129.99m, order.TotalAmount);
    }

    [Fact]
    public async Task GetById_WhenOrderDoesNotExist_ReturnsNull()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var client = new BusinessApiOrderClient(httpClient);

        // Act
        var order = await client.GetByIdAsync("UNKNOWN");

        // Assert
        Assert.Null(order);
    }

    [Fact]
    public async Task GetById_WhenBusinessApiFails_ThrowsHttpRequestException()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.InternalServerError))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var client = new BusinessApiOrderClient(httpClient);

        // Act + Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetByIdAsync("O-2001"));
    }
}
