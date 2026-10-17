using System.Net;

namespace Support.Agent.Tests.TestHelpers;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _responseBody;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string? responseBody = null)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode);

        if (_responseBody is not null)
        {
            response.Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json");
        }

        return Task.FromResult(response);
    }
}
