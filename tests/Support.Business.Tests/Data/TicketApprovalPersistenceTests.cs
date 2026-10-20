using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Support.Business.Api.Data;
using Support.Business.Api.Data.Entities;

namespace Support.Business.Tests.Data;

public class TicketApprovalPersistenceTests
{
    [Fact]
    public async Task PendingApproval_PersistsAcrossContexts()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<SupportDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var setup =
            new SupportDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var ticketId = Guid.NewGuid();
        var approvalId = Guid.NewGuid();

        await using (var write = new SupportDbContext(options))
        {
            write.Tickets.Add(new SupportTicket
            {
                Id = ticketId,
                CustomerId = "C-1002",
                OrderId = "O-2001",
                Subject = "Package not delivered",
                Description = "Customer reports a missing package.",
                Status = TicketStatus.Open
            });

            write.TicketApprovalRequests.Add(
                new TicketApprovalRequest
                {
                    Id = approvalId,
                    TicketId = ticketId,
                    Reason = "The reported delivery issue appears resolved."
                });

            await write.SaveChangesAsync();
        }

        await using (var read =
            new SupportDbContext(options))
        {
            var approval = await read.TicketApprovalRequests
                .AsNoTracking()
                .SingleAsync(a => a.Id == approvalId);

            Assert.Equal(ticketId, approval.TicketId);
            Assert.Equal(
                ApprovalStatus.Pending,
                approval.Status);

            Assert.Null(approval.ReviewedAtUtc);
            Assert.Null(approval.ReviewedBy);

            Assert.Equal(
                "The reported delivery issue appears resolved.",
                approval.Reason);
        }
    }

    [Fact]
    public async Task ApprovingRequest_DoesNotAutomaticallyResolveTicket()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<SupportDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var setup =
            new SupportDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var ticketId = Guid.NewGuid();
        var approvalId = Guid.NewGuid();

        await using (var create =
            new SupportDbContext(options))
        {
            create.Tickets.Add(new SupportTicket
            {
                Id = ticketId,
                CustomerId = "C-1002",
                Subject = "Delivery investigation",
                Description = "Investigate the delivery.",
                Status = TicketStatus.Open
            });

            create.TicketApprovalRequests.Add(
                new TicketApprovalRequest
                {
                    Id = approvalId,
                    TicketId = ticketId,
                    Reason = "Investigation appears complete."
                });

            await create.SaveChangesAsync();
        }

        await using (var update =
            new SupportDbContext(options))
        {
            var approval =
                await update.TicketApprovalRequests
                    .SingleAsync(a => a.Id == approvalId);

            approval.Status = ApprovalStatus.Approved;
            approval.ReviewedAtUtc = DateTimeOffset.UtcNow;
            approval.ReviewedBy = "test-reviewer";

            await update.SaveChangesAsync();
        }

        await using (var verify =
            new SupportDbContext(options))
        {
            var approval =
                await verify.TicketApprovalRequests
                    .AsNoTracking()
                    .SingleAsync(a => a.Id == approvalId);

            var ticket = await verify.Tickets
                .AsNoTracking()
                .SingleAsync(t => t.Id == ticketId);

            Assert.Equal(
                ApprovalStatus.Approved,
                approval.Status);

            Assert.Equal(
                "test-reviewer",
                approval.ReviewedBy);

            Assert.NotNull(approval.ReviewedAtUtc);

            Assert.Equal(
                TicketStatus.Open,
                ticket.Status);
        }
    }
}
