using System.Net;
using System.Net.Http.Json;
using Support.Business.Tests.TestHelpers;
using Support.Contracts.Approvals;
using Support.Contracts.Tickets;

namespace Support.Business.Tests;

public class ResolutionExecutionApiTests :
    IClassFixture<TicketApiTestFixture>
{
    private const string RequesterId =
        "11111111-1111-4111-8111-111111111111";

    private const string ReviewerId =
        "22222222-2222-4222-8222-222222222222";

    private readonly TicketApiTestFixture _fixture;

    public ResolutionExecutionApiTests(
        TicketApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Guid TicketId, Guid ApprovalId)>
        CreatePendingRequestAsync()
    {
        using var creator = _fixture.CreateClient(
            "Tickets.Create",
            RequesterId);

        using var createResponse =
            await creator.PostAsJsonAsync(
                "/api/tickets",
                new CreateTicketRequest(
                    "C-1002",
                    "O-2001",
                    "Delivery issue",
                    "The customer reported a delayed delivery."));

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var ticket = await createResponse.Content
            .ReadFromJsonAsync<TicketResponse>();

        Assert.NotNull(ticket);

        using var requester = _fixture.CreateClient(
            "Tickets.Approval.Request",
            RequesterId);

        using var submitResponse =
            await requester.PostAsJsonAsync(
                $"/api/tickets/{ticket.Id}/resolution-requests",
                new CreateResolutionApprovalRequest(
                    "Customer confirmed delivery."));

        Assert.Equal(
            HttpStatusCode.Created,
            submitResponse.StatusCode);

        var approval = await submitResponse.Content
            .ReadFromJsonAsync<TicketApprovalResponse>();

        Assert.NotNull(approval);

        return (ticket.Id, approval.Id);
    }

    private async Task ReviewAsync(
        Guid approvalId,
        string decision)
    {
        using var reviewer = _fixture.CreateClient(
            "Tickets.Approval.Review",
            ReviewerId);

        using var response = await reviewer.PostAsJsonAsync(
            $"/api/approvals/{approvalId}/review",
            new ReviewResolutionApprovalRequest(decision));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    private async Task<TicketResponse> ReadTicketAsync(
        Guid ticketId)
    {
        using var reader = _fixture.CreateClient(
            "Support.Read");

        var ticket = await reader
            .GetFromJsonAsync<TicketResponse>(
                $"/api/tickets/{ticketId}");

        Assert.NotNull(ticket);
        return ticket;
    }

    [Fact]
    public async Task ApprovedRequest_ResolvesTicket()
    {
        var (ticketId, approvalId) =
            await CreatePendingRequestAsync();

        await ReviewAsync(approvalId, "Approved");

        using var executor = _fixture.CreateClient(
            "Tickets.Resolve");

        using var response = await executor.PostAsync(
            $"/api/approvals/{approvalId}/execute",
            content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<TicketResponse>();

        Assert.NotNull(result);
        Assert.Equal(ticketId, result.Id);
        Assert.Equal("Resolved", result.Status);

        var saved = await ReadTicketAsync(ticketId);

        Assert.Equal("Resolved", saved.Status);
    }

    [Fact]
    public async Task PendingRequest_CannotResolveTicket()
    {
        var (ticketId, approvalId) =
            await CreatePendingRequestAsync();

        using var executor = _fixture.CreateClient(
            "Tickets.Resolve");

        using var response = await executor.PostAsync(
            $"/api/approvals/{approvalId}/execute",
            content: null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var ticket = await ReadTicketAsync(ticketId);
        Assert.Equal("Open", ticket.Status);
    }

    [Fact]
    public async Task RejectedRequest_CannotResolveTicket()
    {
        var (ticketId, approvalId) =
            await CreatePendingRequestAsync();

        await ReviewAsync(approvalId, "Rejected");

        using var executor = _fixture.CreateClient(
            "Tickets.Resolve");

        using var response = await executor.PostAsync(
            $"/api/approvals/{approvalId}/execute",
            content: null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var ticket = await ReadTicketAsync(ticketId);
        Assert.Equal("Open", ticket.Status);
    }

    [Fact]
    public async Task RepeatExecution_Returns409()
    {
        var (_, approvalId) =
            await CreatePendingRequestAsync();

        await ReviewAsync(approvalId, "Approved");

        using var executor = _fixture.CreateClient(
            "Tickets.Resolve");

        using var first = await executor.PostAsync(
            $"/api/approvals/{approvalId}/execute",
            content: null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var second = await executor.PostAsync(
            $"/api/approvals/{approvalId}/execute",
            content: null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);
    }

    [Fact]
    public async Task ReadOnlyUser_CannotExecuteResolution()
    {
        var (ticketId, approvalId) =
            await CreatePendingRequestAsync();

        await ReviewAsync(approvalId, "Approved");

        using var reader = _fixture.CreateClient(
            "Support.Read");

        using var response = await reader.PostAsync(
            $"/api/approvals/{approvalId}/execute",
            content: null);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var ticket = await ReadTicketAsync(ticketId);

        Assert.Equal("Open", ticket.Status);
    }
}
