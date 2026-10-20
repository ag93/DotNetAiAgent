
using System.Net;
using System.Net.Http.Json;
using Support.Business.Tests.TestHelpers;
using Support.Contracts.Tickets;

namespace Support.Business.Tests;

public class TicketApiTests :
    IClassFixture<TicketApiTestFixture>
{
    private readonly TicketApiTestFixture _fixture;

    public TicketApiTests(TicketApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateThenGetTicket_PersistsRecord()
    {
        using var createClient = _fixture.CreateClient("Tickets.Create");

        var request = new CreateTicketRequest(
            "C-1002",
            "O-2001",
            "Package not delivered",
            "Order is shipped but has not arrived.");

        using var createResponse = await createClient.PostAsJsonAsync("/api/tickets", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>();

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Open", created.Status);
        Assert.Equal("C-1002", created.CustomerId);
        Assert.Equal("O-2001", created.OrderId);

        using var readClient = _fixture.CreateClient("Support.Read");

        using var getResponse = await readClient.GetAsync($"/api/tickets/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var retrieved = await getResponse.Content.ReadFromJsonAsync<TicketResponse>();

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(created.Subject, retrieved.Subject);
    }

    [Fact]
    public async Task CreateTicket_WithoutAuthentication_Returns401()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/tickets",
            new CreateTicketRequest("C-1002", null, "Question", "Details"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_WithReadScopeOnly_Returns403()
    {
        using var client = _fixture.CreateClient("Support.Read");

        using var response = await client.PostAsJsonAsync("/api/tickets",
            new CreateTicketRequest("C-1002", null, "Question", "Details"));

        Assert.Equal(
            HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUnknownTicket_Returns404()
    {
        using var client = _fixture.CreateClient("Support.Read");

        using var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Valid subject", "Valid description")]
    [InlineData("C-1002", "", "Valid description")]
    [InlineData("C-1002", "Valid subject", "")]
    public async Task CreateTicket_WithMissingFields_Returns400(
        string customerId,
        string subject,
        string description)
    {
        using var client = _fixture.CreateClient("Tickets.Create");

        using var response = await client.PostAsJsonAsync("/api/tickets",
            new CreateTicketRequest(customerId, null, subject, description));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
