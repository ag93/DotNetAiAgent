using System.Net;
using System.Net.Http.Json;
using Support.Contracts.Customers;

namespace Support.Operations.Services;

public sealed class BusinessApiCustomerClient
{
    private readonly HttpClient _httpClient;

    public BusinessApiCustomerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CustomerResponse?> GetByIdAsync(string customerId,  CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/customers/{Uri.EscapeDataString(customerId)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CustomerResponse>(cancellationToken: cancellationToken);
    }
}
