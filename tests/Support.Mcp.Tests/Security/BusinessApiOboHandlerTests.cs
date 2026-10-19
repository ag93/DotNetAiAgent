using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Moq;
using Support.Mcp.Server.Security;

namespace Support.Mcp.Tests.Security;

public class BusinessApiOboHandlerTests
{
    private const string Scope = "api://business-api-test-id/Support.Read";

    private static ClaimsPrincipal CreateUser()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "mcp-test-user")
            ],
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }

    private static IHttpContextAccessor CreateContextAccessor(ClaimsPrincipal? user = null)
    {
        var context = new DefaultHttpContext();

        if (user is not null)
        {
            context.User = user;
        }

        return new HttpContextAccessor
        {
            HttpContext = context
        };
    }

    private static IConfiguration CreateConfiguration(bool includeScope = true)
    {
        var values = new Dictionary<string, string?>();

        if (includeScope)
        {
            values["BusinessApi:Scope"] = Scope;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(respond(request));
        }
    }

    [Fact]
    public async Task SendAsync_AttachesDelegatedBearerToken()
    {
        var user = CreateUser();
        var tokenAcquisition = new Mock<ITokenAcquisition>();

        tokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.Is<IEnumerable<string>>(
                    scopes => scopes.SequenceEqual(
                        new[] { Scope })),
                null,
                null,
                user,
                null))
            .ReturnsAsync("fake-mcp-test-token");

        string? observedScheme = null;
        string? observedToken = null;

        var downstream = new StubHttpMessageHandler(
            request =>
            {
                observedScheme =
                    request.Headers.Authorization?.Scheme;

                observedToken =
                    request.Headers.Authorization?.Parameter;

                return new HttpResponseMessage(
                    HttpStatusCode.OK);
            });

        var handler = new BusinessApiOboHandler(
            tokenAcquisition.Object,
            CreateContextAccessor(user),
            CreateConfiguration())
        {
            InnerHandler = downstream
        };

        using var client = new HttpClient(handler);

        using var response = await client.GetAsync(
            "https://business.example.test/api/customers/C-1002");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal("Bearer", observedScheme);
        Assert.Equal("fake-mcp-test-token", observedToken);

        tokenAcquisition.Verify(
            x => x.GetAccessTokenForUserAsync(
                It.Is<IEnumerable<string>>(
                    scopes => scopes.SequenceEqual(
                        new[] { Scope })),
                null,
                null,
                user,
                null),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithoutAuthenticatedUser_Throws()
    {
        var tokenAcquisition = new Mock<ITokenAcquisition>();
        var downstreamCalled = false;

        var downstream = new StubHttpMessageHandler(
            _ =>
            {
                downstreamCalled = true;

                return new HttpResponseMessage(
                    HttpStatusCode.OK);
            });

        var handler = new BusinessApiOboHandler(
            tokenAcquisition.Object,
            CreateContextAccessor(),
            CreateConfiguration())
        {
            InnerHandler = downstream
        };

        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAsync(
                "https://business.example.test/api/customers/C-1002"));

        Assert.False(downstreamCalled);
        tokenAcquisition.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_WithoutScopeConfiguration_Throws()
    {
        var user = CreateUser();
        var tokenAcquisition = new Mock<ITokenAcquisition>();
        var downstreamCalled = false;

        var downstream = new StubHttpMessageHandler(
            _ =>
            {
                downstreamCalled = true;

                return new HttpResponseMessage(
                    HttpStatusCode.OK);
            });

        var handler = new BusinessApiOboHandler(
            tokenAcquisition.Object,
            CreateContextAccessor(user),
            CreateConfiguration(includeScope: false))
        {
            InnerHandler = downstream
        };

        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAsync(
                "https://business.example.test/api/orders/O-2001"));

        Assert.False(downstreamCalled);
        tokenAcquisition.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_Downstream401_RemainsUnauthorized()
    {
        var user = CreateUser();
        var tokenAcquisition = new Mock<ITokenAcquisition>();

        tokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                null,
                null,
                user,
                null))
            .ReturnsAsync("fake-mcp-test-token");

        var downstreamCalls = 0;

        var downstream = new StubHttpMessageHandler(
            _ =>
            {
                downstreamCalls++;

                return new HttpResponseMessage(
                    HttpStatusCode.Unauthorized);
            });

        var handler = new BusinessApiOboHandler(
            tokenAcquisition.Object,
            CreateContextAccessor(user),
            CreateConfiguration())
        {
            InnerHandler = downstream
        };

        using var client = new HttpClient(handler);

        using var response = await client.GetAsync(
            "https://business.example.test/api/orders/O-2001");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(1, downstreamCalls);
    }
}
