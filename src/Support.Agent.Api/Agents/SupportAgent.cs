using Microsoft.Extensions.AI;
using Support.Agent.Api.Tools;
using Support.Operations.Tools;

namespace Support.Agent.Api.Agents;

public sealed class SupportAgent
{
    private readonly IChatClient _chatClient;
    private readonly CustomerLookupTool _customerTool;
    private readonly OrderLookupTool _orderTool;

    public SupportAgent(IChatClient chatClient, CustomerLookupTool customerTool, OrderLookupTool orderTool)
    {
        _chatClient = chatClient;
        _customerTool = customerTool;
        _orderTool = orderTool;
    }

    public async Task<SupportAgentResult> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("A message is required.", nameof(message));
        
        var invokedTools = new List<string>();

        async Task<object> GetCustomerAsync(string customerId)
        {
            invokedTools.Add("get_customer");
            var customer = await _customerTool.ExecuteAsync(customerId, cancellationToken);

            return customer is null
                ? new { found = false, customerId }
                : new { found = true, customer };
        }

        async Task<object> GetOrderAsync(string orderId)
        {
            invokedTools.Add("get_order");
            var order = await _orderTool.ExecuteAsync(orderId, cancellationToken);

            return order is null
                ? new { found = false, orderId }
                : new { found = true, order };
        }

        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(
                    (Func<string, Task<object>>)GetCustomerAsync,
                    "get_customer",
                    "Look up a customer by customer ID."),

                AIFunctionFactory.Create(
                    (Func<string, Task<object>>)GetOrderAsync,
                    "get_order",
                    "Look up an order by order ID.")
            ]
        };

        var messages = new List<ChatMessage>
        {
            new(
                ChatRole.System,
                "You are a customer support assistant. " +
                "Use get_customer for customer record questions. " +
                "Use get_order for order record questions. " +
                "Never invent customer or order information. " +
                "If a requested record is not found, say so."),

            new(ChatRole.User, message)
        };

        var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
        return new SupportAgentResult(response.Text, invokedTools.ToArray());
    }
}

public sealed record SupportAgentResult(string? Reply, IReadOnlyList<string> InvokedTools);
