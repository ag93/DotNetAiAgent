using Support.Contracts.Tickets;
using Support.Operations.Services;

namespace Support.Operations.Tools;

public sealed class TicketLookupTool(BusinessApiTicketClient ticketClient)
{
    public async Task<TicketResponse?> GetTicketAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ticketId?.Trim(), out var id) ||
            id == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid ticket ID is required.",
                nameof(ticketId));
        }

        return await ticketClient.GetByIdAsync(id, cancellationToken);
    }
}
