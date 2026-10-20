namespace Support.Agent.Api.Requests;

public sealed record DraftTicketRequest(string CustomerId, string? OrderId, string Issue);
