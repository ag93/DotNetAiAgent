using Support.Business.Api.Data.Entities;
using Support.Contracts.Approvals;

namespace Support.Business.Api.Approvals;

public static class ApprovalMappings
{
    public static TicketApprovalResponse ToResponse(this TicketApprovalRequest approval)
    {
        return new TicketApprovalResponse(
            approval.Id,
            approval.TicketId,
            approval.Reason,
            approval.Status.ToString(),
            approval.RequestedAtUtc,
            approval.ReviewedAtUtc,
            approval.ReviewedBy);
    }
}
