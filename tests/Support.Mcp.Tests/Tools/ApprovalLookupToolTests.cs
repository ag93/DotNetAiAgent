
using System.Net;
using System.Net.Http.Json;
using Support.Contracts.Approvals;
using Support.Operations.Services;
using Support.Operations.Tools;

namespace Support.Mcp.Tests.Tools;

public class ApprovalLookupToolTests
{
    private sealed class StubHttpHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(respond(request));
        }
    }

    private static ApprovalLookupTool CreateTool(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var httpClient = new HttpClient(
            new StubHttpHandler(respond))
        {
            BaseAddress = new Uri(
                "https://business.example.test/")
        };

        return new ApprovalLookupTool(
            new BusinessApiApprovalClient(httpClient));
    }

    [Fact]
    public async Task ExistingApproval_ReturnsVerifiedStatus()
    {
        var approvalId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        var expected = new TicketApprovalResponse(
            approvalId,
            ticketId,
            "Customer confirmed the issue was resolved.",
            "Approved",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "reviewer-object-id");

        var tool = CreateTool(request =>
        {
            Assert.Equal(
                $"/api/approvals/{approvalId:D}",
                request.RequestUri!.AbsolutePath);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            };
        });

        var actual = await tool.GetApprovalAsync(
            approvalId.ToString());

        Assert.NotNull(actual);
        Assert.Equal(approvalId, actual.Id);
        Assert.Equal(ticketId, actual.TicketId);
        Assert.Equal("Approved", actual.Status);
    }

    [Fact]
    public async Task MissingApproval_ReturnsNull()
    {
        var tool = CreateTool(_ =>
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var result = await tool.GetApprovalAsync(
            Guid.NewGuid().ToString());

        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidApprovalId_ThrowsBeforeHttpCall()
    {
        var wasCalled = false;

        var tool = CreateTool(_ =>
        {
            wasCalled = true;

            return new HttpResponseMessage(
                HttpStatusCode.OK);
        });

        await Assert.ThrowsAsync<ArgumentException>(
            () => tool.GetApprovalAsync("invalid-id"));

        Assert.False(wasCalled);
    }

    [Fact]
    public async Task Unauthorized_IsNotTreatedAsMissing()
    {
        var tool = CreateTool(_ =>
            new HttpResponseMessage(
                HttpStatusCode.Forbidden));

        var exception =
            await Assert.ThrowsAsync<HttpRequestException>(
                () => tool.GetApprovalAsync(
                    Guid.NewGuid().ToString()));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            exception.StatusCode);
    }
}
