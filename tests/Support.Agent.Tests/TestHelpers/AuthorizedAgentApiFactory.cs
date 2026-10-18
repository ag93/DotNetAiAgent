using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Support.Agent.Tests.TestHelpers;

public sealed class AuthorizedAgentApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        // Supply non-secret, test-only configuration.
        // No real Entra tenant or Ollama server is required.
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                    ["AzureAd:TenantId"] = "11111111-1111-1111-1111-111111111111",
                    ["AzureAd:ClientId"] = "22222222-2222-2222-2222-222222222222",
                    ["BusinessApi:BaseUrl"] = "https://localhost/",
                    ["Ollama:BaseUrl"] = "http://localhost:11434",
                    ["Ollama:Model"] = "llama3.2:3b",
                    ["McpServer:Endpoint"] = "http://localhost:5105/mcp"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultForbidScheme = "Test";
                })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthenticationHandler>(
                    "Test",
                    _ => { });
        });
    }

    public HttpClient CreateAuthorizedClient(string scope = "Agent.Invoke")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "agent-test-user");
        client.DefaultRequestHeaders.Add("X-Test-Scope", scope);

        return client;
    }
}
