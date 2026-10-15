using Support.Business.Api.Customers;

namespace Support.Business.Api.Services;

public sealed class CustomerService
{
    private readonly Dictionary<string, CustomerResponse> _customers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["C-1002"] = new("C-1002", "Alex Morgan", "alex@example.com"),

            ["C-1003"] = new("C-1003", "Jordan Lee", "jordan@example.com")
        };

    public CustomerResponse? GetById(string customerId)
    {
        _customers.TryGetValue(customerId, out var customer);
        return customer;
    }
}
