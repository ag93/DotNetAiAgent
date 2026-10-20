
using System.Net;
using System.Net.Http.Json;
using Support.Agent.Tests.TestHelpers;
using Support.Contracts.Tickets;

namespace Support.Agent.Tests;

public class TicketCreationEndpointTests : IClassFixture<AuthorizedAgentApiFactory>
{
    private readonly AuthorizedAgentApiFactory _factory;

    public TicketCreationEndpointTests(AuthorizedAgentApiFactory factory)
    {
        _factory = factory;
    }

    private static object CreateRequest(bool confirmed) => new
    {
        ticket = new CreateTicketRequest(
            "C-1002",
            "O-2001",
            "Package not delivered",
            "The package has not arrived."),
        confirmed
    };

    [Fact]
    public async Task AnonymousCreationRequest_Returns401()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/agent/tickets", CreateRequest(confirmed: true));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Preview_DoesNotCreateTicket()
    {
        using var client = _factory.CreateAuthorizedClient();

        using var response = await client.PostAsJsonAsync("/api/agent/tickets", CreateRequest(confirmed: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await System.Text.Json.JsonDocument
            .ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.True(
            json.RootElement
                .GetProperty("requiresConfirmation")
                .GetBoolean());

        Assert.Equal(
            "Package not delivered",
            json.RootElement
                .GetProperty("proposedTicket")
                .GetProperty("subject")
                .GetString());
    }

    [Fact]
    public async Task InvalidTicket_Returns400()
    {
        using var client = _factory.CreateAuthorizedClient();

        using var response = await client.PostAsJsonAsync("/api/agent/tickets",
            new
            {
                ticket = new CreateTicketRequest(
                    "C-1002",
                    null,
                    "",
                    "Description"),
                confirmed = true
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
