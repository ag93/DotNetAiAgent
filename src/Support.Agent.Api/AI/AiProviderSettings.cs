using Microsoft.Extensions.Configuration;

namespace Support.Agent.Api.AI;

public sealed record FoundrySettings(Uri Endpoint, string Deployment);

public static class AiProviderSettings
{
    public static string GetProvider(IConfiguration configuration)
    {
        var provider = configuration["AI:Provider"]?.Trim();

        return provider?.ToLowerInvariant() switch
        {
            null or "" or "ollama" => "Ollama",
            "foundry" => "Foundry",

            _ => throw new InvalidOperationException(
                $"Unsupported AI provider '{provider}'. " +
                "Use Ollama or Foundry.")
        };
    }

    public static FoundrySettings GetFoundrySettings(IConfiguration configuration)
    {
        var endpoint = configuration["Foundry:Endpoint"]?.Trim();

        var deployment = configuration["Foundry:Deployment"]?.Trim();

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Foundry:Endpoint must be a valid HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(deployment))
        {
            throw new InvalidOperationException("Foundry:Deployment is required.");
        }

        return new FoundrySettings(uri, deployment);
    }
}
