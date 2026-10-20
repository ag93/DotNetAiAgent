namespace Support.Contracts.Tickets;

public sealed record CreateTicketRequest(string CustomerId, string? OrderId, string Subject, string Description);
