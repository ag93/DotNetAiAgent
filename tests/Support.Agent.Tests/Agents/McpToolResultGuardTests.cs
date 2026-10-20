using ModelContextProtocol.Protocol;
using Support.Agent.Api.Agents;

namespace Support.Agent.Tests.Agents;

public class McpToolResultGuardTests
{
    [Fact]
    public void SuccessfulResult_IsPreserved()
    {
        var result = new CallToolResult
        {
            IsError = false,
            Content =
            [
                new TextContentBlock
                {
                    Text = """{"found":true,"customerId":"C-1002"}"""
                }
            ]
        };

        var sanitized = McpToolResultGuard.Sanitize(result);

        Assert.Same(result, sanitized);
    }

    [Fact]
    public void NotFoundResult_IsNotTreatedAsError()
    {
        var result = new CallToolResult
        {
            IsError = false,
            Content =
            [
                new TextContentBlock
                {
                    Text = """{"found":false,"customerId":"C-9999"}"""
                }
            ]
        };

        var sanitized = McpToolResultGuard.Sanitize(result);

        Assert.Same(result, sanitized);
    }

    [Fact]
    public void ToolError_ReplacesSensitiveDetails()
    {
        var result = new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = "Internal exception at https://internal.example.test"
                }
            ]
        };

        var sanitized = McpToolResultGuard.Sanitize(result);

        Assert.True(sanitized.IsError == true);
        Assert.Single(sanitized.Content);

        var text = Assert.IsType<TextContentBlock>(sanitized.Content[0]).Text;

        Assert.Contains("lookup failed", text);
        Assert.DoesNotContain("internal.example.test", text);
        Assert.Contains("Do not interpret this failure as a missing record", text);
    }

    [Fact]
    public void UnspecifiedErrorFlag_IsPreserved()
    {
        var result = new CallToolResult
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = """{"found":true}"""
                }
            ]
        };

        var sanitized = McpToolResultGuard.Sanitize(result);

        Assert.Same(result, sanitized);
    }
}
