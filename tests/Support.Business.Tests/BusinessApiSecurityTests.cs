
using System.Net;
using Support.Business.Tests.TestHelpers;

namespace Support.Business.Tests;

public class BusinessApiSecurityTests : IClassFixture<AuthorizedBusinessApiFactory>
{
    private readonly AuthorizedBusinessApiFactory _factory;

    public BusinessApiSecurityTests(AuthorizedBusinessApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/customers/C-1002")]
    [InlineData("/api/orders/O-2001")]
    public async Task Request_WithoutAuthentication_Returns401(string path)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/customers/C-1002")]
    [InlineData("/api/orders/O-2001")]
    public async Task Request_WithoutSupportRead_Returns403(string path)
    {
        using var client = _factory.CreateAuthorizedClient(scope: "Other.Read");
        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
