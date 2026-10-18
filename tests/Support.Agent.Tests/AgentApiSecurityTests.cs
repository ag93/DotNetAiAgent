using System.Net;
using System.Net.Http.Json;
using Support.Agent.Tests.TestHelpers;

namespace Support.Agent.Tests;

public class AgentApiSecurityTests : IClassFixture<AuthorizedAgentApiFactory>
{
    private readonly AuthorizedAgentApiFactory _factory;

    public AgentApiSecurityTests(AuthorizedAgentApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("GET", "/api/agent/customers/C-1002")]
    [InlineData("GET", "/api/agent/orders/O-2001")]
    [InlineData("POST", "/api/agent/chat-test")]
    [InlineData("POST", "/api/agent/customer-chat")]
    [InlineData("POST", "/api/agent/mcp-chat")]
    public async Task AnonymousRequest_Returns401(string method, string path)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/agent/customers/C-1002")]
    [InlineData("GET", "/api/agent/orders/O-2001")]
    [InlineData("POST", "/api/agent/chat-test")]
    [InlineData("POST", "/api/agent/customer-chat")]
    [InlineData("POST", "/api/agent/mcp-chat")]
    public async Task AuthenticatedWithoutAgentInvoke_Returns403(string method, string path)
    {
        using var client = _factory.CreateAuthorizedClient(scope: "Other.Read");

        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedRequest_ReachesEndpointValidation()
    {
        using var client = _factory.CreateAuthorizedClient();

        // Empty message should be rejected by endpoint validation,
        // not by authorization.
        var response = await client.PostAsJsonAsync("/api/agent/chat-test", new { message = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
