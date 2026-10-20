namespace Support.Business.Api.Data.Entities;
public enum TicketStatus
{
    Open,
    InProgress,
    Resolved,
    Closed
}

public sealed class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CustomerId { get; set; } = string.Empty;

    public string? OrderId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
