using System.Text.Json;
namespace Support.Agent.Api.Agents;

public static class ResolutionDraftParser
{
    private sealed record DraftFields(string? Reason);

    public static bool TryParse(string? modelOutput, out string? reason)
    {
        reason = null;

        if (string.IsNullOrWhiteSpace(modelOutput))
        {
            return false;
        }

        try
        {
            var fields = JsonSerializer.Deserialize<DraftFields>(
                modelOutput,
                new JsonSerializerOptions(
                    JsonSerializerDefaults.Web));

            var proposedReason = fields?.Reason?.Trim();

            if (string.IsNullOrWhiteSpace(proposedReason) ||
                proposedReason.Length > 1000)
            {
                return false;
            }

            reason = proposedReason;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
