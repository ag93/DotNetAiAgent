using System.Net;
using Microsoft.Extensions.AI;
using Support.Agent.Api.Agents;
using Support.Agent.Tests.TestHelpers;
using Support.Operations.Services;
using Support.Operations.Tools;

namespace Support.Agent.Tests.Agents;

public class SupportAgentTests
{
    [Fact]
    public async Task AskAsync_WhenModelRequestsCustomer_ExecutesCustomerTool()
    {
        // Arrange
        const string customerJson = """
        {
            "id": "C-1002",
            "name": "Alex Morgan",
            "email": "alex@example.com"
        }
        """;

        using var customerHttp = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, customerJson))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        using var orderHttp = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        using var ticketLookupHttp = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var customerTool = new CustomerLookupTool(new BusinessApiCustomerClient(customerHttp));
        var orderTool = new OrderLookupTool(new BusinessApiOrderClient(orderHttp));
        var ticketLookupTool = new TicketLookupTool(new BusinessApiTicketClient(ticketLookupHttp));
        using var fakeModel = new FakeToolCallingChatClient(); //Defaults to use customer lookup tool
        using var chatClient = new ChatClientBuilder(fakeModel).UseFunctionInvocation().Build();
        var agent = new SupportAgent(chatClient, customerTool, orderTool, ticketLookupTool);

        // Act
        var result = await agent.AskAsync("Look up customer C-1002.");

        // Assert
        Assert.Contains("get_customer", result.InvokedTools);
        Assert.DoesNotContain("get_order", result.InvokedTools);
        Assert.True(fakeModel.ReceivedToolResult);
        Assert.Equal(2, fakeModel.RequestCount);
    }

    [Fact]
    public async Task AskAsync_WhenModelRequestsOrder_ExecutesOrderTool()
    {
        // Arrange
        const string orderJson = """
        {
            "id": "O-2001",
            "customerId": "C-1002",
            "status": "Shipped",
            "totalAmount": 129.99,
            "placedAt": "2026-10-10T14:30:00+00:00"
        }
        """;

        using var customerHttp = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        using var orderHttp = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, orderJson))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        using var ticketHttp = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, orderJson))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var customerTool = new CustomerLookupTool(new BusinessApiCustomerClient(customerHttp));
        var orderTool = new OrderLookupTool(new BusinessApiOrderClient(orderHttp));
        var ticketTool = new TicketLookupTool(new BusinessApiTicketClient(ticketHttp));

        using var fakeModel = new FakeToolCallingChatClient(toolName: "get_order", argumentName: "orderId", argumentValue: "O-2001");

        using var chatClient = new ChatClientBuilder(fakeModel)
            .UseFunctionInvocation()
            .Build();

        var agent = new SupportAgent(chatClient, customerTool, orderTool, ticketTool);

        // Act
        var result = await agent.AskAsync("What is the status of order O-2001?");

        // Assert
        Assert.Contains("get_order", result.InvokedTools);
        Assert.DoesNotContain("get_customer", result.InvokedTools);
        Assert.True(fakeModel.ReceivedToolResult);
        Assert.Equal(2, fakeModel.RequestCount);
    }
}
