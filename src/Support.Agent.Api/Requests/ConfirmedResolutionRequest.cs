namespace Support.Agent.Api.Requests;

public sealed record ConfirmedResolutionRequest(Guid TicketId, string Reason, bool Confirmed);
