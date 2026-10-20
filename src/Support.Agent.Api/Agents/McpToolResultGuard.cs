using ModelContextProtocol.Protocol;

namespace Support.Agent.Api.Agents;

public static class McpToolResultGuard
{
    public static CallToolResult Sanitize(CallToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsError != true)
        {
            return result;
        }

        return new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text =
                        "The lookup failed. No record data was verified. " +
                        "Do not interpret this failure as a missing record."
                }
            ]
        };
    }
}
