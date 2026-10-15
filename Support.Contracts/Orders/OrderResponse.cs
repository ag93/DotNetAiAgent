
namespace Support.Contracts.Orders;

public sealed record OrderResponse(string Id, string CustomerId, string Status, decimal TotalAmount, DateTimeOffset PlacedAt);