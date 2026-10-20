using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Support.Operations.Tools;

namespace Support.Mcp.Server.Tools;

[McpServerToolType]
public sealed class TicketMcpTool(TicketLookupTool ticketLookupTool)
{
    [McpServerTool(Name = "get_ticket")]
    [Description(
        "Retrieve an existing support ticket by its GUID. " +
        "Use this to check a ticket's status or details. " +
        "This tool does not create or modify tickets.")]
    public async Task<string> GetTicketAsync([Description("The support ticket GUID.")] string ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await ticketLookupTool.GetTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return JsonSerializer.Serialize(new
            {
                found = false,
                ticketId
            });
        }
        return JsonSerializer.Serialize(new
        {
            found = true,
            ticket
        });
    }
}
