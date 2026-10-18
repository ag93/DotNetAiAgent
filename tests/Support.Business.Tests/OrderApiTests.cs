using Microsoft.AspNetCore.Mvc.Testing;
using Support.Business.Tests.TestHelpers;
using Support.Contracts.Orders;
using System.Net;
using System.Net.Http.Json;

namespace Support.Business.Tests;

public class OrderApiTests : IClassFixture<AuthorizedBusinessApiFactory>
{
    private readonly HttpClient _client;
    public OrderApiTests(AuthorizedBusinessApiFactory factory)
    {
        _client = factory.CreateAuthorizedClient();
    }

    [Fact]
    public async Task GetOrder_WhenOrderExists_ReturnsOrder()
    {
        var response = await _client.GetAsync("/api/orders/O-2001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.NotNull(order);
        Assert.Equal("O-2001", order.Id);
        Assert.Equal("Shipped", order.Status);
    }

    [Fact]
    public async Task GetOrder_WhenOrderDoesNotExist_Returns404()
    {
        var response = await _client.GetAsync("/api/orders/UNKNOWN");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
