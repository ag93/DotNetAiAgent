
using System.Net;
using System.Net.Http.Json;
using Support.Business.Tests.TestHelpers;
using Support.Contracts.Approvals;
using Support.Contracts.Tickets;

namespace Support.Business.Tests;

public class ResolutionApprovalReviewApiTests :
    IClassFixture<TicketApiTestFixture>
{
    private const string RequesterId =
        "11111111-1111-4111-8111-111111111111";

    private const string ReviewerId =
        "22222222-2222-4222-8222-222222222222";

    private readonly TicketApiTestFixture _fixture;

    public ResolutionApprovalReviewApiTests(
        TicketApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Guid TicketId, Guid ApprovalId)>
        CreatePendingApprovalAsync()
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
                    "Delivery follow-up",
                    "Customer requested investigation."));

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
                    "Customer confirmed the issue is resolved."));

        Assert.Equal(
            HttpStatusCode.Created,
            submitResponse.StatusCode);

        var approval = await submitResponse.Content
            .ReadFromJsonAsync<TicketApprovalResponse>();

        Assert.NotNull(approval);

        return (ticket.Id, approval.Id);
    }

    [Fact]
    public async Task IndependentReviewer_CanApprove()
    {
        var (ticketId, approvalId) =
            await CreatePendingApprovalAsync();

        using var reviewer = _fixture.CreateClient(
            "Tickets.Approval.Review",
            ReviewerId);

        using var response = await reviewer.PostAsJsonAsync(
            $"/api/approvals/{approvalId}/review",
            new ReviewResolutionApprovalRequest("Approved"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var approval = await response.Content
            .ReadFromJsonAsync<TicketApprovalResponse>();

        Assert.NotNull(approval);
        Assert.Equal("Approved", approval.Status);
        Assert.Equal(ReviewerId, approval.ReviewedBy);
        Assert.NotNull(approval.ReviewedAtUtc);

        using var reader = _fixture.CreateClient(
            "Support.Read");

        var ticket = await reader
            .GetFromJsonAsync<TicketResponse>(
                $"/api/tickets/{ticketId}");

        Assert.NotNull(ticket);
        Assert.Equal("Open", ticket.Status);
    }

    [Fact]
    public async Task IndependentReviewer_CanReject()
    {
        var (_, approvalId) =
            await CreatePendingApprovalAsync();

        using var reviewer = _fixture.CreateClient(
            "Tickets.Approval.Review",
            ReviewerId);

        using var response = await reviewer.PostAsJsonAsync(
            $"/api/approvals/{approvalId}/review",
            new ReviewResolutionApprovalRequest("Rejected"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var approval = await response.Content
            .ReadFromJsonAsync<TicketApprovalResponse>();

        Assert.NotNull(approval);
        Assert.Equal("Rejected", approval.Status);
        Assert.Equal(ReviewerId, approval.ReviewedBy);
    }

    [Fact]
    public async Task Requester_CannotApproveOwnProposal()
    {
        var (_, approvalId) =
            await CreatePendingApprovalAsync();

        using var requester = _fixture.CreateClient(
            "Tickets.Approval.Review",
            RequesterId);

        using var response = await requester.PostAsJsonAsync(
            $"/api/approvals/{approvalId}/review",
            new ReviewResolutionApprovalRequest("Approved"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        using var reader = _fixture.CreateClient(
            "Support.Read");

        var approval = await reader
            .GetFromJsonAsync<TicketApprovalResponse>(
                $"/api/approvals/{approvalId}");

        Assert.NotNull(approval);
        Assert.Equal("Pending", approval.Status);
    }

    [Fact]
    public async Task ReadOnlyUser_CannotReview()
    {
        var (_, approvalId) =
            await CreatePendingApprovalAsync();

        using var reader = _fixture.CreateClient(
            "Support.Read",
            ReviewerId);

        using var response = await reader.PostAsJsonAsync(
            $"/api/approvals/{approvalId}/review",
            new ReviewResolutionApprovalRequest("Approved"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task AlreadyReviewedApproval_CannotBeReviewedAgain()
    {
        var (_, approvalId) =
            await CreatePendingApprovalAsync();

        using var reviewer = _fixture.CreateClient(
            "Tickets.Approval.Review",
            ReviewerId);

        using var first = await reviewer.PostAsJsonAsync(
            $"/api/approvals/{approvalId}/review",
            new ReviewResolutionApprovalRequest("Approved"));

        Assert.Equal(
            HttpStatusCode.OK,
            first.StatusCode);

        using var second = await reviewer.PostAsJsonAsync(
            $"/api/approvals/{approvalId}/review",
            new ReviewResolutionApprovalRequest("Rejected"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);
    }
}
