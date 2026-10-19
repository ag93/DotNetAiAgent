using System.Net.Http.Headers;
using Microsoft.Identity.Web;

namespace Support.Agent.Api.Security;

public sealed class BusinessApiOboHandler : DelegatingHandler
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public BusinessApiOboHandler(ITokenAcquisition tokenAcquisition, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _tokenAcquisition = tokenAcquisition;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("An authenticated user is required for Business API access.");
        
        var scope = _configuration["BusinessApi:Scope"];

        if (string.IsNullOrWhiteSpace(scope))
            throw new InvalidOperationException("BusinessApi:Scope is not configured.");
        
        var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync([scope], user: user);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
