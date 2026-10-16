using System.Net;
using Support.Contracts.Orders;

namespace Support.Agent.Api.Services;

public sealed class BusinessApiOrderClient
{
    private readonly HttpClient _httpClient;

    public BusinessApiOrderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OrderResponse?> GetByIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/orders/{Uri.EscapeDataString(orderId)}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken: cancellationToken);
    }
}
