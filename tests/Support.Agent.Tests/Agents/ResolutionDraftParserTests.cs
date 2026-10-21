
using Support.Agent.Api.Agents;

namespace Support.Agent.Tests.Agents;

public class ResolutionDraftParserTests
{
    [Fact]
    public void ValidReason_IsAcceptedAndTrimmed()
    {
        const string json =
            """
            {
              "reason": "  Customer reports delivery is complete.  "
            }
            """;

        var success = ResolutionDraftParser.TryParse(
            json,
            out var reason);

        Assert.True(success);
        Assert.Equal(
            "Customer reports delivery is complete.",
            reason);
    }

    [Fact]
    public void InvalidJson_IsRejected()
    {
        var success = ResolutionDraftParser.TryParse(
            "The ticket should be resolved.",
            out var reason);

        Assert.False(success);
        Assert.Null(reason);
    }

    [Fact]
    public void MissingReason_IsRejected()
    {
        var success = ResolutionDraftParser.TryParse(
            """{"status":"Approved"}""",
            out var reason);

        Assert.False(success);
        Assert.Null(reason);
    }

    [Fact]
    public void OverlongReason_IsRejected()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            new
            {
                reason = new string('x', 1001)
            });

        var success = ResolutionDraftParser.TryParse(
            json,
            out var reason);

        Assert.False(success);
        Assert.Null(reason);
    }
}
