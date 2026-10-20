using Microsoft.Extensions.AI;
using Support.Operations.Tools;

namespace Support.Agent.Api.Agents;

public sealed class SupportAgent
{
    private readonly IChatClient _chatClient;
    private readonly CustomerLookupTool _customerTool;
    private readonly OrderLookupTool _orderTool;
    private readonly TicketLookupTool _ticketLookupTool;

    public SupportAgent(
        IChatClient chatClient,
        CustomerLookupTool customerTool,
        OrderLookupTool orderTool,
        TicketLookupTool ticketLookupTool)
    {
        _chatClient = chatClient;
        _customerTool = customerTool;
        _orderTool = orderTool;
        _ticketLookupTool = ticketLookupTool;
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

        async Task<object> GetTicketAsync(string ticketId)
        {
            invokedTools.Add("get_ticket");
            var ticket = await _ticketLookupTool.GetTicketAsync(ticketId, cancellationToken);

            return ticket is null
                ? new { found = false, ticketId }
                : new { found = true, ticket };
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
                    "Look up an order by order ID."),

                AIFunctionFactory.Create(
                    (Func<string, Task<object>>)GetTicketAsync,
                    "get_ticket",
                    "Retrieve an existing support ticket by its GUID. " +
                    "Use this to check a ticket's status or details. " +
                    "This tool does not create or modify tickets."),
            ]
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
        return new SupportAgentResult(response.Text, invokedTools.ToArray());
    }
}

public sealed record SupportAgentResult(string? Reply, IReadOnlyList<string> InvokedTools);
