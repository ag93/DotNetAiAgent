using Support.Contracts.Tickets;

namespace Support.Agent.Api.Requests;

public sealed record ConfirmedTicketCreateRequest(CreateTicketRequest Ticket, bool Confirmed);