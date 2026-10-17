using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace Support.Agent.Api.Agents;

public sealed class McpSupportAgent
{
    private readonly IChatClient _chatClient;
    private readonly IConfiguration _configuration;

    public McpSupportAgent(IChatClient chatClient, IConfiguration configuration)
    {
        _chatClient = chatClient;
        _configuration = configuration;
    }

    public async Task<McpSupportAgentResult> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A message is required.", nameof(message));
        }

        var endpoint = _configuration["McpServer:Endpoint"]
            ?? throw new InvalidOperationException("McpServer:Endpoint is not configured.");

        var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(endpoint),
                TransportMode = HttpTransportMode.StreamableHttp
            });

        await using var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        var discoveredTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

        // Never expose every discovered tool automatically.
        // These are the only MCP tools approved for this agent.
        var allowedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "get_customer",
            "get_order"
        };

        var allowedTools = discoveredTools.Where(tool => allowedNames.Contains(tool.Name)).ToList();

        var options = new ChatOptions
        {
            Tools = [.. allowedTools]
        };

        var messages = new List<ChatMessage>
        {
            new(
                ChatRole.System,
                "You are a customer support assistant. " +
                "Use get_customer for customer record questions " +
                "and get_order for order record questions. " +
                "Never invent customer or order details. " +
                "If a record is missing, say so. " +
                "Do not claim an order was paid unless the " +
                "tool result explicitly confirms payment."),

            new(ChatRole.User, message)
        };

        var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
        return new McpSupportAgentResult(response.Text, allowedTools.Select(tool => tool.Name).ToArray());
    }
}

public sealed record McpSupportAgentResult(string? Reply, IReadOnlyList<string> AvailableTools);
