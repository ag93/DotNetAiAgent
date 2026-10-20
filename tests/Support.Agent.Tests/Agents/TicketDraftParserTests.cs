
using Support.Agent.Api.Agents;

namespace Support.Agent.Tests.Agents;

public class TicketDraftParserTests
{
    [Fact]
    public void ValidDraft_ReturnsTicketDetails()
    {
        const string json =
            """
            {
              "subject": "Package not delivered",
              "description": "The package has not arrived."
            }
            """;

        var success = TicketDraftParser.TryParse(
            json,
            "C-1002",
            "O-2001",
            out var ticket);

        Assert.True(success);
        Assert.NotNull(ticket);
        Assert.Equal(
            "Package not delivered",
            ticket.Subject);
        Assert.Equal("C-1002", ticket.CustomerId);
        Assert.Equal("O-2001", ticket.OrderId);
    }

    [Fact]
    public void InvalidJson_IsRejected()
    {
        var success = TicketDraftParser.TryParse(
            "This is not JSON",
            "C-1002",
            null,
            out var ticket);

        Assert.False(success);
        Assert.Null(ticket);
    }

    [Fact]
    public void MissingSubject_IsRejected()
    {
        var success = TicketDraftParser.TryParse(
            """{"description":"A delivery issue."}""",
            "C-1002",
            null,
            out var ticket);

        Assert.False(success);
        Assert.Null(ticket);
    }

    [Fact]
    public void ModelCannotOverrideCustomerOrOrderIds()
    {
        const string json =
            """
            {
              "customerId": "C-9999",
              "orderId": "O-9999",
              "subject": "Delivery issue",
              "description": "Customer reports a delay."
            }
            """;

        var success = TicketDraftParser.TryParse(
            json,
            "C-1002",
            "O-2001",
            out var ticket);

        Assert.True(success);
        Assert.NotNull(ticket);

        Assert.Equal("C-1002", ticket.CustomerId);
        Assert.Equal("O-2001", ticket.OrderId);
    }
}
