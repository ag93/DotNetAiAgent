namespace Support.Agent.Api.Requests;

public sealed record DraftResolutionRequest(Guid TicketId, string Explanation);
