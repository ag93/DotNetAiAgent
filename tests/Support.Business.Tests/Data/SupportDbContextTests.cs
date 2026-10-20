using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Support.Business.Api.Data;
using Support.Business.Api.Data.Entities;

namespace Support.Business.Tests.Data;

public class SupportDbContextTests
{
    [Fact]
    public async Task Ticket_IsPersistedAcrossDbContexts()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<SupportDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var setupContext = new SupportDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        var ticketId = Guid.NewGuid();

        await using (var writeContext = new SupportDbContext(options))
        {
            writeContext.Tickets.Add(new SupportTicket
            {
                Id = ticketId,
                CustomerId = "C-1002",
                OrderId = "O-2001",
                Subject = "Package not delivered",
                Description =
                    "The order is marked shipped but has not arrived."
            });

            await writeContext.SaveChangesAsync();
        }

        await using (var readContext = new SupportDbContext(options))
        {
            var ticket = await readContext.Tickets
                .AsNoTracking()
                .SingleAsync(t => t.Id == ticketId);

            Assert.Equal("C-1002", ticket.CustomerId);
            Assert.Equal("O-2001", ticket.OrderId);
            Assert.Equal("Package not delivered", ticket.Subject);
            Assert.Equal(TicketStatus.Open, ticket.Status);
        }
    }

    [Fact]
    public async Task TicketStatusUpdate_IsPersisted()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SupportDbContext>().UseSqlite(connection).Options;

        await using (var setupContext = new SupportDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        var ticketId = Guid.NewGuid();

        await using (var createContext = new SupportDbContext(options))
        {
            createContext.Tickets.Add(new SupportTicket
            {
                Id = ticketId,
                CustomerId = "C-1002",
                Subject = "Order delivery question",
                Description = "Customer requests an update."
            });

            await createContext.SaveChangesAsync();
        }

        await using (var updateContext = new SupportDbContext(options))
        {
            var ticket = await updateContext.Tickets.SingleAsync(t => t.Id == ticketId);

            ticket.Status = TicketStatus.InProgress;
            ticket.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await updateContext.SaveChangesAsync();
        }

        await using (var verifyContext = new SupportDbContext(options))
        {
            var ticket = await verifyContext.Tickets
                .AsNoTracking()
                .SingleAsync(t => t.Id == ticketId);

            Assert.Equal(TicketStatus.InProgress, ticket.Status);
        }
    }
}
