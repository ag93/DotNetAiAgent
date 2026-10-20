using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

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

    public async Task<McpSupportAgentResult> AskAsync(string message, string mcpAccessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mcpAccessToken))
        {
            throw new ArgumentException("An MCP access token is required.", nameof(mcpAccessToken));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A message is required.", nameof(message));
        }

        var endpoint = _configuration["McpServer:Endpoint"]
            ?? throw new InvalidOperationException("McpServer:Endpoint is not configured.");

        var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(endpoint),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string> 
                {
                    ["Authorization"] = $"Bearer {mcpAccessToken}"
                }
            });

        await using var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        var discoveredTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

        // Never expose every discovered tool automatically.
        // These are the only MCP tools approved for this agent.
        var allowedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "get_customer",
            "get_order",
            "get_ticket"
        };

        var allowedTools = discoveredTools.Where(tool => allowedNames.Contains(tool.Name)).ToList();

        var cache = new RequestToolResultCache<CallToolResult>();
        var budget = new RequestToolCallBudget(maximumCalls: 4);
        async Task<CallToolResult> GetCustomerAsync(string customerId)
        {
            return await cache.GetOrAddAsync("get_customer", customerId,
                async () =>
                {
                    budget.Consume();
                    var result = await mcpClient.CallToolAsync(
                        "get_customer",
                        new Dictionary<string, object?>
                        {
                            ["customerId"] = customerId.Trim()
                        },
                        cancellationToken: cancellationToken).AsTask();
                    return McpToolResultGuard.Sanitize(result);
                },
                result => result.IsError != true,
                cancellationToken);
        }

        async Task<CallToolResult> GetOrderAsync(string orderId)
        {
            return await cache.GetOrAddAsync("get_order", orderId,
                async () => 
                {
                    budget.Consume();
                    var result = await mcpClient.CallToolAsync(
                        "get_order",
                        new Dictionary<string, object?>
                        {
                            ["orderId"] = orderId.Trim()
                        },
                        cancellationToken: cancellationToken).AsTask();
                    return McpToolResultGuard.Sanitize(result);
                },
                result => result.IsError != true,
                cancellationToken);
        }


        async Task<CallToolResult> GetTicketAsync(string ticketId)
        {
            return await cache.GetOrAddAsync("get_ticket", ticketId,
                async () =>
                {
                    budget.Consume();
                    var result = await mcpClient.CallToolAsync(
                        "get_ticket",
                        new Dictionary<string, object?>
                        {
                            ["ticketId"] = ticketId.Trim()
                        },
                        cancellationToken: cancellationToken);
                    return McpToolResultGuard.Sanitize(result);
                },
                result => result.IsError != true,
                cancellationToken);
        }


        var executableTools = new List<AITool>();

        foreach (var tool in allowedTools)
        {
            if (tool.Name == "get_customer")
            {
                executableTools.Add(
                    AIFunctionFactory.Create(
                        (Func<string, Task<CallToolResult>>)GetCustomerAsync,
                        "get_customer",
                        tool.Description));
            }
            else if (tool.Name == "get_order")
            {
                executableTools.Add(
                    AIFunctionFactory.Create(
                        (Func<string, Task<CallToolResult>>)GetOrderAsync,
                        "get_order",
                        tool.Description));
            }
            else if (tool.Name == "get_ticket")
            {
                executableTools.Add(AIFunctionFactory.Create(
                    (Func<string, Task<CallToolResult>>)GetTicketAsync,
                    "get_ticket",
                    tool.Description));
            }

        }

        var options = new ChatOptions
        {
            Tools = executableTools
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
                "tool result explicitly confirms payment." +
                "When using customer or order lookup tools," +
                "only report details explicitly returned by successful tool results." +
                "If a lookup returns found: false, explain that the requested record was not found." +
                "If a tool result indicates an error, explain that the lookup could not be completed" +
                "and do not claim the record does not exist." +
                "Never invent customer details, order statuses, payment confirmations," +
                "or other information that has not been verified by the tools." +
                "Use `get_ticket` when the user asks about an existing support ticket and provides its ticket ID. " +
                "Report only the ticket fields returned by the successful lookup. If the ticket was not found, say so. " +
                "If the lookup failed, explain that its status could not be verified. " +
                "Do not invent ticket IDs, statuses, updates, or approval decisions. " +
                "`get_ticket` is read-only; never claim to create, update, resolve, or close tickets through this tool."),

            new(ChatRole.User, message)
        };

        var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
        return new McpSupportAgentResult(response.Text, allowedTools.Select(tool => tool.Name).ToArray());
    }
}

public sealed record McpSupportAgentResult(string? Reply, IReadOnlyList<string> AvailableTools);
