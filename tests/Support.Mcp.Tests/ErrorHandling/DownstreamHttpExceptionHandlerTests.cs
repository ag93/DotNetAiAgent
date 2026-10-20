using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Support.Mcp.Server.ErrorHandling;

namespace Support.Mcp.Tests.ErrorHandling;

public class DownstreamHttpExceptionHandlerTests
{
    private static DownstreamHttpExceptionHandler CreateHandler()
    {
        return new DownstreamHttpExceptionHandler(NullLogger<DownstreamHttpExceptionHandler>.Instance);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();

        context.Request.Path = "/mcp";
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static async Task<JsonDocument> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task DownstreamAuthorizationFailure_Returns502(HttpStatusCode downstreamStatus)
    {
        var handler = CreateHandler();
        var context = CreateContext();

        var exception = new HttpRequestException(
            "Sensitive downstream error",
            inner: null,
            statusCode: downstreamStatus);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status502BadGateway, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        using var json = await ReadResponseAsync(context);
        Assert.Equal(502, json.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("Sensitive downstream error", json.RootElement.ToString());
    }

    [Fact]
    public async Task ConnectionFailure_Returns503()
    {
        var handler = CreateHandler();
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context,
            new HttpRequestException("Internal connection details"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        using var json = await ReadResponseAsync(context);
        Assert.Equal(503, json.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("Internal connection details", json.RootElement.ToString());
    }

    [Fact]
    public async Task UnrelatedException_IsNotHandled()
    {
        var handler = CreateHandler();
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context,
            new InvalidOperationException("Programming error"),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }
}
