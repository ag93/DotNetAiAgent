using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Support.Contracts.Customers;
using Support.Business.Tests.TestHelpers;

namespace Support.Business.Tests;

public class CustomerApiTests : IClassFixture<AuthorizedBusinessApiFactory>
{
    private readonly HttpClient _client;
    public CustomerApiTests(AuthorizedBusinessApiFactory factory)
    {
        _client = factory.CreateAuthorizedClient();
    }

    [Fact]
    public async Task GetCustomer_WhenCustomerExists_ReturnsCustomer()
    {
        // Act
        var response = await _client.GetAsync("/api/customers/C-1002");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.NotNull(customer);
        Assert.Equal("C-1002", customer.Id);
        Assert.Equal("Alex Morgan", customer.Name);
    }

    [Fact]
    public async Task GetCustomer_WhenCustomerDoesNotExist_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/customers/UNKNOWN");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
