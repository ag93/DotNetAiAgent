namespace Support.Business.Api.Data.Entities;
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public sealed class TicketApprovalRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public SupportTicket Ticket { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
}
