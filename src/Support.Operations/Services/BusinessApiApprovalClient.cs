using System.Net;
using System.Net.Http.Json;
using Support.Contracts.Approvals;

namespace Support.Operations.Services;

public sealed class BusinessApiApprovalClient(HttpClient httpClient)
{
    public async Task<TicketApprovalResponse?> GetByIdAsync(Guid approvalId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/approvals/{approvalId:D}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var approval = await response.Content.ReadFromJsonAsync<TicketApprovalResponse>(cancellationToken);

        if (approval is null)
        {
            throw new HttpRequestException("Downstream approval response was empty.");
        }

        return approval;
    }
}
