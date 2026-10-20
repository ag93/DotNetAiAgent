using Support.Business.Api.Data.Entities;
using Support.Contracts.Tickets;

namespace Support.Business.Api.Tickets;

public static class TicketMappings
{
    public static TicketResponse ToResponse(this SupportTicket ticket)
    {
        return new TicketResponse(
            ticket.Id,
            ticket.CustomerId,
            ticket.OrderId,
            ticket.Subject,
            ticket.Description,
            ticket.Status.ToString(),
            ticket.CreatedAtUtc,
            ticket.UpdatedAtUtc);
    }
}
