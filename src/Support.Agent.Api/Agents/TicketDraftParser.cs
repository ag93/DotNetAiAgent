using System.Text.Json;
using Support.Contracts.Tickets;

namespace Support.Agent.Api.Agents;

public static class TicketDraftParser
{
    private sealed record DraftFields(string? Subject, string? Description);

    public static bool TryParse(string? modelOutput, string customerId, string? orderId, out CreateTicketRequest? ticket)
    {
        ticket = null;

        if (string.IsNullOrWhiteSpace(modelOutput))
            return false;

        try
        {
            var fields = JsonSerializer.Deserialize<DraftFields>(
                modelOutput,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            var subject = fields?.Subject?.Trim();
            var description = fields?.Description?.Trim();

            if (string.IsNullOrWhiteSpace(subject) ||
                subject.Length > 200 ||
                string.IsNullOrWhiteSpace(description) ||
                description.Length > 4000)
            {
                return false;
            }

            ticket = new CreateTicketRequest(customerId, orderId, subject, description);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
