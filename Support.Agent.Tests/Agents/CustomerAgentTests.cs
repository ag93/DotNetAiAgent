using System.Net;
using Microsoft.Extensions.AI;
using Support.Agent.Api.Agents;
using Support.Agent.Api.Services;
using Support.Agent.Api.Tools;
using Support.Agent.Tests.TestHelpers;

namespace Support.Agent.Tests.Agents;

public class CustomerAgentTests
{
    [Fact]
    public async Task AskAsync_WhenModelRequestsCustomer_ExecutesTool()
    {
        // Arrange: simulate the Business API's response.
        const string responseBody = """
        {
            "id": "C-1002",
            "name": "Alex Morgan",
            "email": "alex@example.com"
        }
        """;

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var customerClient = new BusinessApiCustomerClient(httpClient);
        var customerTool = new CustomerLookupTool(customerClient);

        // Arrange: simulate the language model.
        using var fakeModel = new FakeToolCallingChatClient();
        using var chatClient = new ChatClientBuilder(fakeModel).UseFunctionInvocation().Build();

        var agent = new CustomerAgent(chatClient, customerTool);

        // Act
        var result = await agent.AskAsync("Look up customer C-1002.");

        // Assert
        Assert.True(result.ToolInvoked);
        Assert.Equal("C-1002", result.RequestedCustomerId);
        Assert.Equal("Customer lookup completed.", result.Reply);
        Assert.True(fakeModel.ReceivedToolResult);
        Assert.Equal(2, fakeModel.RequestCount);
    }
}
