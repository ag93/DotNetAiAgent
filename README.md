### Local development configuration

Each API uses an `appsettings.Development.json` file for machine-specific settings. These files are excluded from Git.

Before running the solution, configure the relevant services:

- **Support.Agent.Api:** Microsoft Entra tenant and application IDs, Ollama endpoint and model, Business API URL, and MCP server URL.
- **Support.Business.Api:** Microsoft Entra tenant and application IDs.
- **Support.Mcp.Server:** Business API URL.

Do not commit client secrets, access tokens, or other credentials. Use .NET User Secrets or environment variables for sensitive local configuration.

The application requires access to appropriately configured Microsoft Entra app registrations for authenticated requests.