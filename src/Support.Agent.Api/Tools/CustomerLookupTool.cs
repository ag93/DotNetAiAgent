using Support.Agent.Api.Services;
using Support.Contracts.Customers;

namespace Support.Agent.Api.Tools;

public sealed class CustomerLookupTool
{
    private readonly BusinessApiCustomerClient _customerClient;

    public CustomerLookupTool(BusinessApiCustomerClient customerClient)
    {
        _customerClient = customerClient;
    }

    public async Task<CustomerResponse?> ExecuteAsync(string customerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new ArgumentException("Customer ID is required.", nameof(customerId));
        }

        return await _customerClient.GetByIdAsync(customerId.Trim(), cancellationToken);
    }
}
