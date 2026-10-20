namespace Support.Contracts.Approvals;

public sealed record TicketApprovalResponse(
    Guid Id,
    Guid TicketId,
    string Reason,
    string Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    string? ReviewedBy);
