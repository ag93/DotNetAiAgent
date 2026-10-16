using Microsoft.Extensions.AI;
using Support.Agent.Api.Tools;

namespace Support.Agent.Api.Agents;

public sealed class CustomerAgent
{
    private readonly IChatClient _chatClient;
    private readonly CustomerLookupTool _customerTool;

    public CustomerAgent(IChatClient chatClient, CustomerLookupTool customerTool)
    {
        _chatClient = chatClient;
        _customerTool = customerTool;
    }

    public async Task<CustomerAgentResult> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A message is required.", nameof(message));
        }

        var toolInvoked = false;
        string? requestedCustomerId = null;

        async Task<object> GetCustomerAsync(string customerId)
        {
            toolInvoked = true;
            requestedCustomerId = customerId;

            var customer = await _customerTool.ExecuteAsync(customerId, cancellationToken);

            return customer is null
                ? new { found = false, customerId }
                : new { found = true, customer };
        }

        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(
                    GetCustomerAsync,
                    "get_customer",
                    "Look up a customer by customer ID. Use this tool for questions about customer records.")
            ]
        };

        var messages = new List<ChatMessage>
        {
            new(
                ChatRole.System,
                "You are a customer support assistant. " +
                "Use the get_customer tool when asked for customer " +
                "information. Never invent customer details. " +
                "If a customer is not found, say so."),

            new(ChatRole.User, message)
        };

        var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);

        return new CustomerAgentResult(response.Text, toolInvoked, requestedCustomerId);
    }
}

public sealed record CustomerAgentResult(string? Reply, bool ToolInvoked, string? RequestedCustomerId);
