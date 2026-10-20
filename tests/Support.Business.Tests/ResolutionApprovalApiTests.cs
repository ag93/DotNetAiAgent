using System.Net;
using System.Net.Http.Json;
using Support.Business.Tests.TestHelpers;
using Support.Contracts.Approvals;
using Support.Contracts.Tickets;

namespace Support.Business.Tests;

public class ResolutionApprovalApiTests : IClassFixture<TicketApiTestFixture>
{
    private readonly TicketApiTestFixture _fixture;

    public ResolutionApprovalApiTests(TicketApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> CreateOpenTicketAsync()
    {
        using var client = _fixture.CreateClient("Tickets.Create");

        using var response = await client.PostAsJsonAsync(
            "/api/tickets",
            new CreateTicketRequest(
                "C-1002",
                "O-2001",
                "Delivery follow-up",
                "Customer requests a delivery update."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
         
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();

        Assert.NotNull(ticket);

        return ticket.Id;
    }

    [Fact]
    public async Task SubmitAndRetrieve_PersistsPendingApproval()
    {
        var ticketId = await CreateOpenTicketAsync();

        using var requester = _fixture.CreateClient("Tickets.Approval.Request");

        using var submitResponse =
            await requester.PostAsJsonAsync(
                $"/api/tickets/{ticketId}/resolution-requests",
                new CreateResolutionApprovalRequest(
                    "Customer confirmed delivery."));

        Assert.Equal(HttpStatusCode.Created, submitResponse.StatusCode);

        var created = await submitResponse.Content.ReadFromJsonAsync<TicketApprovalResponse>();

        Assert.NotNull(created);
        Assert.Equal(ticketId, created.TicketId);
        Assert.Equal("Pending", created.Status);
        Assert.Null(created.ReviewedBy);
        Assert.Null(created.ReviewedAtUtc);

        using var reader = _fixture.CreateClient("Support.Read");

        using var getResponse = await reader.GetAsync($"/api/approvals/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var retrieved = await getResponse.Content.ReadFromJsonAsync<TicketApprovalResponse>();

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Pending", retrieved.Status);

        // Submitting approval did not resolve the ticket.
        var ticket = await reader.GetFromJsonAsync<TicketResponse>($"/api/tickets/{ticketId}");

        Assert.NotNull(ticket);
        Assert.Equal("Open", ticket.Status);
    }

    [Fact]
    public async Task UnknownTicket_Returns404()
    {
        using var client = _fixture.CreateClient("Tickets.Approval.Request");

        using var response = await client.PostAsJsonAsync($"/api/tickets/{Guid.NewGuid()}/resolution-requests",
            new CreateResolutionApprovalRequest("Proposed resolution."));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DuplicatePendingRequest_Returns409()
    {
        var ticketId = await CreateOpenTicketAsync();

        using var client = _fixture.CreateClient("Tickets.Approval.Request");

        var request = new CreateResolutionApprovalRequest("Customer confirmed delivery.");

        using var first = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/resolution-requests", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var second = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/resolution-requests", request);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task AnonymousSubmission_Returns401()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/resolution-requests",
            new CreateResolutionApprovalRequest("Reason"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReadOnlyScopeCannotSubmit_Returns403()
    {
        using var client = _fixture.CreateClient("Support.Read");

        using var response = await client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/resolution-requests",
            new CreateResolutionApprovalRequest("Reason"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EmptyReason_Returns400()
    {
        using var client = _fixture.CreateClient("Tickets.Approval.Request");

        using var response = await client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/resolution-requests",
            new CreateResolutionApprovalRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
