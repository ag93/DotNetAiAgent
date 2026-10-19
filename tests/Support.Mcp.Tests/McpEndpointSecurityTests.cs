using Support.Mcp.Tests.TestHelpers;
using System.Net;
using System.Net.Http.Json;

namespace Support.Mcp.Tests;

public class McpEndpointSecurityTests : IClassFixture<AuthorizedMcpServerFactory>
{
    private readonly AuthorizedMcpServerFactory _factory;

    public McpEndpointSecurityTests(AuthorizedMcpServerFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_WithoutToken_Returns200()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/mcp", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_WithoutRequiredScope_Returns403()
    {
        using var client = _factory.CreateAuthorizedClient(scope: "Other.Read");
        using var response = await client.PostAsJsonAsync("/mcp", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


    [Fact]
    public async Task McpGet_IsNotSupported()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/mcp");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

}
