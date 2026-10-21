
using Microsoft.Extensions.Configuration;
using Support.Agent.Api.AI;

namespace Support.Agent.Tests.AI;

public class AiProviderSettingsTests
{
    private static IConfiguration CreateConfig(
        Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void MissingProvider_DefaultsToOllama()
    {
        var configuration = CreateConfig(
            new Dictionary<string, string?>());

        var provider =
            AiProviderSettings.GetProvider(configuration);

        Assert.Equal("Ollama", provider);
    }

    [Fact]
    public void UnsupportedProvider_IsRejected()
    {
        var configuration = CreateConfig(
            new Dictionary<string, string?>
            {
                ["AI:Provider"] = "UnknownProvider"
            });

        Assert.Throws<InvalidOperationException>(
            () => AiProviderSettings.GetProvider(
                configuration));
    }

    [Fact]
    public void FoundrySettings_RequireValidEndpointAndDeployment()
    {
        var invalidConfiguration = CreateConfig(
            new Dictionary<string, string?>
            {
                ["Foundry:Endpoint"] = "not-a-url",
                ["Foundry:Deployment"] = ""
            });

        Assert.Throws<InvalidOperationException>(
            () => AiProviderSettings.GetFoundrySettings(
                invalidConfiguration));

        var validConfiguration = CreateConfig(
            new Dictionary<string, string?>
            {
                ["Foundry:Endpoint"] =
                    "https://example.openai.azure.com/",
                ["Foundry:Deployment"] = "test-deployment"
            });

        var settings =
            AiProviderSettings.GetFoundrySettings(
                validConfiguration);

        Assert.Equal(
            "test-deployment",
            settings.Deployment);
    }
}
