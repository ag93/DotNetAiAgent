namespace Support.Contracts.Tickets;

public sealed record TicketResponse(
    Guid Id,
    string CustomerId,
    string? OrderId,
    string Subject,
    string Description,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
