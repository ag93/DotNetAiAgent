using Microsoft.Extensions.AI;

namespace Support.Agent.Tests.TestHelpers;

public sealed class FakeToolCallingChatClient : IChatClient
{
    public int RequestCount { get; private set; }
    public bool ReceivedToolResult { get; private set; }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        RequestCount++;
        var toolMessage = messages.FirstOrDefault(message => message.Role == ChatRole.Tool);

        if (toolMessage is null)
        {
            // First response: pretend the model requests a tool.
            var tools = options?.Tools;

            if (tools is null || !tools.Any(tool => tool.Name == "get_customer"))
            {
                throw new InvalidOperationException("The get_customer tool was not provided.");
            }

            return Task.FromResult(
                new ChatResponse(
                    new ChatMessage(
                        ChatRole.Assistant,
                        [
                            new FunctionCallContent(
                                "call-1",
                                "get_customer",
                                new Dictionary<string, object?>
                                {
                                    ["customerId"] = "C-1002"
                                })
                        ])));
        }

        // Second response: the middleware has executed the tool.
        ReceivedToolResult = toolMessage.Contents
            .OfType<FunctionResultContent>()
            .Any(result => result.CallId == "call-1");

        return Task.FromResult(
            new ChatResponse(
                new ChatMessage(
                    ChatRole.Assistant,
                    "Customer lookup completed.")));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
                                                                          ChatOptions? options = null,
                                                                          CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Streaming is not needed for these tests.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
    }
}
