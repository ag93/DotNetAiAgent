
using System.Net;
using System.Net.Http.Json;
using Support.Contracts.Tickets;
using Support.Operations.Services;
using Support.Operations.Tools;

namespace Support.Mcp.Tests.Tools;

public class TicketLookupToolTests
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

    private static TicketLookupTool CreateTool(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var httpClient = new HttpClient(
            new StubHttpHandler(respond))
        {
            BaseAddress = new Uri(
                "https://business.example.test/")
        };

        return new TicketLookupTool(
            new BusinessApiTicketClient(httpClient));
    }

    [Fact]
    public async Task ExistingTicket_ReturnsPersistedData()
    {
        var id = Guid.NewGuid();

        var expected = new TicketResponse(
            id,
            "C-1002",
            "O-2001",
            "Package not delivered",
            "The package has not arrived.",
            "Open",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var tool = CreateTool(request =>
        {
            Assert.Equal(
                $"/api/tickets/{id:D}",
                request.RequestUri!.AbsolutePath);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            };
        });

        var actual = await tool.GetTicketAsync(
            id.ToString());

        Assert.NotNull(actual);
        Assert.Equal(id, actual.Id);
        Assert.Equal("C-1002", actual.CustomerId);
        Assert.Equal("Open", actual.Status);
    }

    [Fact]
    public async Task MissingTicket_ReturnsNull()
    {
        var tool = CreateTool(_ =>
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var result = await tool.GetTicketAsync(
            Guid.NewGuid().ToString());

        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidTicketId_ThrowsBeforeHttpRequest()
    {
        var wasCalled = false;

        var tool = CreateTool(_ =>
        {
            wasCalled = true;

            return new HttpResponseMessage(
                HttpStatusCode.OK);
        });

        await Assert.ThrowsAsync<ArgumentException>(
            () => tool.GetTicketAsync("not-a-guid"));

        Assert.False(wasCalled);
    }

    [Fact]
    public async Task DownstreamUnauthorized_IsNotTreatedAsMissing()
    {
        var tool = CreateTool(_ =>
            new HttpResponseMessage(
                HttpStatusCode.Unauthorized));

        var exception =
            await Assert.ThrowsAsync<HttpRequestException>(
                () => tool.GetTicketAsync(
                    Guid.NewGuid().ToString()));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            exception.StatusCode);
    }
}
