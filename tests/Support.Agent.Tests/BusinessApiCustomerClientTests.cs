using System.Net;
using System.Text.Json;
using Support.Agent.Tests.TestHelpers;
using Support.Contracts.Customers;
using Support.Operations.Services;

namespace Support.Agent.Tests;

public class BusinessApiCustomerClientTests
{
    [Fact]
    public async Task GetById_WhenCustomerExists_ReturnsCustomer()
    {
        // Arrange
        var expectedCustomer = new CustomerResponse("C-1002", "Alex Morgan", "alex@example.com");
        var responseBody = JsonSerializer.Serialize(expectedCustomer);

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var client = new BusinessApiCustomerClient(httpClient);

        // Act
        var customer = await client.GetByIdAsync("C-1002");

        // Assert
        Assert.NotNull(customer);
        Assert.Equal("C-1002", customer.Id);
        Assert.Equal("Alex Morgan", customer.Name);
    }

    [Fact]
    public async Task GetById_WhenCustomerDoesNotExist_ReturnsNull()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var client = new BusinessApiCustomerClient(httpClient);

        // Act
        var customer = await client.GetByIdAsync("UNKNOWN");

        // Assert
        Assert.Null(customer);
    }

    [Fact]
    public async Task GetById_WhenBusinessApiFails_ThrowsHttpRequestException()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.InternalServerError))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var client = new BusinessApiCustomerClient(httpClient);

        // Act + Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetByIdAsync("C-1002"));
    }
}
