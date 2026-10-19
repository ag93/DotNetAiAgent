
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Support.Mcp.Tests.TestHelpers;

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Test-User"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var scope = Request.Headers["X-Test-Scope"].ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "mcp-test-user")
        };

        if (!string.IsNullOrWhiteSpace(scope))
        {
            claims.Add(new Claim("scp", scope));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
