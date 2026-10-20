using System.Net;
using System.Net.Http.Json;
using Support.Contracts.Tickets;

namespace Support.Operations.Services;

public sealed class BusinessApiTicketClient(HttpClient httpClient)
{
    public async Task<TicketResponse?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/tickets/{ticketId:D}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TicketResponse>(cancellationToken);
    }
}
