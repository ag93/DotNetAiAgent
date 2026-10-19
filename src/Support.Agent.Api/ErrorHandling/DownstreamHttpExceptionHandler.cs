
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Support.Agent.Api.ErrorHandling;

public sealed class DownstreamHttpExceptionHandler(ILogger<DownstreamHttpExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not HttpRequestException httpException)
        {
            return false;
        }

        var statusCode = httpException.StatusCode.HasValue
            ? StatusCodes.Status502BadGateway
            : StatusCodes.Status503ServiceUnavailable;

        logger.LogWarning("Downstream HTTP failure. Status: {DownstreamStatus}, TraceId: {TraceId}",
            httpException.StatusCode.HasValue
                ? (int)httpException.StatusCode.Value
                : null,
            httpContext.TraceIdentifier);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status502BadGateway
                ? "Downstream service request failed"
                : "Downstream service unavailable",
            Detail = "The support service could not complete a downstream request.",
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;


        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problem,
            options: new JsonSerializerOptions(JsonSerializerDefaults.Web),
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}
