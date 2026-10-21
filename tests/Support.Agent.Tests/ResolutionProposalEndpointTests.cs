using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Support.Agent.Api.Requests;
using Support.Agent.Tests.TestHelpers;

namespace Support.Agent.Tests;

public class ResolutionProposalEndpointTests :
    IClassFixture<AuthorizedAgentApiFactory>
{
    private readonly AuthorizedAgentApiFactory _factory;

    public ResolutionProposalEndpointTests(
        AuthorizedAgentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousProposal_Returns401()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/agent/resolution-requests",
            new ConfirmedResolutionRequest(
                Guid.NewGuid(),
                "Customer confirmed delivery.",
                Confirmed: true));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Preview_ReturnsProposalWithoutSubmitting()
    {
        var ticketId = Guid.NewGuid();

        using var client = _factory.CreateAuthorizedClient();

        using var response = await client.PostAsJsonAsync(
            "/api/agent/resolution-requests",
            new ConfirmedResolutionRequest(
                ticketId,
                "  Customer confirmed delivery.  ",
                Confirmed: false));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());

        Assert.True(
            json.RootElement
                .GetProperty("requiresConfirmation")
                .GetBoolean());

        Assert.Equal(
            ticketId.ToString("D"),
            json.RootElement
                .GetProperty("ticketId")
                .GetGuid()
                .ToString("D"));

        Assert.Equal(
            "Customer confirmed delivery.",
            json.RootElement
                .GetProperty("proposedReason")
                .GetString());
    }

    [Fact]
    public async Task EmptyReason_Returns400()
    {
        using var client = _factory.CreateAuthorizedClient();

        using var response = await client.PostAsJsonAsync(
            "/api/agent/resolution-requests",
            new ConfirmedResolutionRequest(
                Guid.NewGuid(),
                "   ",
                Confirmed: true));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}
