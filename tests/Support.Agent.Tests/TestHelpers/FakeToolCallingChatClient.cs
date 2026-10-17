using Microsoft.Extensions.AI;

namespace Support.Agent.Tests.TestHelpers;

public sealed class FakeToolCallingChatClient : IChatClient
{
    public int RequestCount { get; private set; }
    public bool ReceivedToolResult { get; private set; }
    private readonly string _toolName;
    private readonly string _argumentName;
    private readonly string _argumentValue;

    public FakeToolCallingChatClient(string toolName = "get_customer", string argumentName = "customerId", string argumentValue = "C-1002")
    {
        _toolName = toolName;
        _argumentName = argumentName;
        _argumentValue = argumentValue;
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        RequestCount++;
        var toolMessage = messages.FirstOrDefault(message => message.Role == ChatRole.Tool);

        if (toolMessage is null)
        {
            // First response: pretend the model requests a tool.
            var tools = options?.Tools;

            if (tools is null || !tools.Any(tool => tool.Name == _toolName))
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
                                _toolName,
                                new Dictionary<string, object?>
                                {
                                    [_argumentName] = _argumentValue
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
