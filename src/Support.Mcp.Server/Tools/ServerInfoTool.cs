using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Support.Mcp.Server.Tools;

[McpServerToolType]
public sealed class ServerInfoTool
{
    [McpServerTool(Name = "server_info")]
    [Description("Returns basic diagnostic information about the support MCP server.")]
    public static string GetServerInfo()
    {
        return "Support MCP Server is running.";
    }
}
