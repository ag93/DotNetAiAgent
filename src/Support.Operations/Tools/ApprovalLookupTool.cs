
using Support.Contracts.Approvals;
using Support.Operations.Services;

namespace Support.Operations.Tools;

public sealed class ApprovalLookupTool(BusinessApiApprovalClient approvalClient)
{
    public async Task<TicketApprovalResponse?> GetApprovalAsync(string approvalId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(approvalId?.Trim(), out var id) || id == Guid.Empty)
        {
            throw new ArgumentException("A valid approval ID is required.", nameof(approvalId));
        }

        return await approvalClient.GetByIdAsync(id, cancellationToken);
    }
}
