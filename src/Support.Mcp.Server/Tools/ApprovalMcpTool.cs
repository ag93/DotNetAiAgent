
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Support.Operations.Tools;

namespace Support.Mcp.Server.Tools;

[McpServerToolType]
public sealed class ApprovalMcpTool(ApprovalLookupTool approvalLookupTool)
{
    [McpServerTool(Name = "get_approval")]
    [Description(
        "Look up an existing ticket-resolution approval " +
        "by its GUID. Returns its decision status and " +
        "related ticket ID. This tool is read-only.")]
    public async Task<string> GetApprovalAsync([Description("The resolution approval GUID.")] string approvalId,
        CancellationToken cancellationToken = default)
    {
        var approval = await approvalLookupTool.GetApprovalAsync(approvalId, cancellationToken);

        if (approval is null)
        {
            return JsonSerializer.Serialize(new
            {
                found = false,
                approvalId
            });
        }

        return JsonSerializer.Serialize(new
        {
            found = true,
            approval = new
            {
                approval.Id,
                approval.TicketId,
                approval.Status,
                approval.Reason,
                approval.RequestedAtUtc,
                approval.ReviewedAtUtc
            }
        });
    }
}
