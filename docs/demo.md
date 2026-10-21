# .NET AI Support Agent — Portfolio Demonstration

## Objective

Demonstrate a model-independent .NET 10 AI support agent that retrieves verified business information, helps a user draft and confirm a ticket, submits a proposed resolution for independent human approval, and reports the actual persisted outcome. Both native `IChatClient` tools and MCP-backed tools are supported.

## Prerequisites

- .NET 10 solution builds and the automated test suite passes (baseline: 126 tests).
- Local development settings and .NET User Secrets are configured.
- `Support.Business.Api` is running at its configured HTTPS address.
- `Support.Mcp.Server` is running with the protected Streamable HTTP endpoint at `http://localhost:5105/mcp`.
- `Support.Agent.Api` is running at its configured HTTPS address.
- Ollama is running with the existing local chat model, or `AI:Provider` is set to `Foundry` with working deployment and Azure credentials.
- Swagger authentication is configured for the Agent API and Business API.
- A requester can authenticate for `Agent.Invoke`, `Tickets.Create`, and `Tickets.Approval.Request` (via Agent API OBO). A **different identity** is needed to demonstrate a real independent review. The reviewer requires `Tickets.Approval.Review`; authorized execution requires `Tickets.Resolve`.
- Sample customer `C-1002` and order `O-2001` are available. Use a new ticket for the demonstration to avoid collisions with already resolved tickets or pending approvals.

## Demonstration at a glance

| Scene | What to show | Evidence |
|---|---|---|
| 1 — Read-only agent | Agent uses `get_customer` and `get_order` to answer from the Business API | Tool invocation and grounded result |
| 2 — AI ticket draft | Model prepares subject/description without writing | Draft output; no ticket created |
| 3 — Confirm ticket | Explicit confirmation creates a persisted support ticket | Created ticket ID and subsequent `get_ticket` |
| 4 — AI resolution draft | Model proposes a resolution reason without changing state | Draft response |
| 5 — Confirm proposal | User confirms and creates a `Pending` approval | Approval ID; ticket remains open |
| 6 — Human review | Different authorized reviewer approves proposal | Persisted `Approved` decision; ticket still open |
| 7 — Controlled execution | Authorized execution resolves ticket | Ticket becomes `Resolved` |
| 8 — Verified AI summary | Agent invokes `get_approval` and `get_ticket` | Correctly distinguishes decision and ticket state |
| Optional — Model portability | Switch `AI:Provider` from Ollama to Foundry and restart Agent API | Same read-only tool works without business-code changes |

## Core walkthrough script

### Scene 1 — Verified lookup via native or MCP chat

1. Authenticate to Agent API Swagger using `Agent.Invoke`.
2. Call `POST /api/agent/customer-chat` or `POST /api/agent/mcp-chat` using the request schema shown in Swagger. Example message: `Look up customer C-1002 and order O-2001. Tell me only what the records confirm.`
3. Show the response and inspect logs to confirm actual `get_customer` and `get_order` invocation. The model must not invent payment confirmation or other unverified data.
4. Briefly explain that the same `Support.Operations` lookups work through native and MCP paths.

### Scene 2 — Draft and confirm a new support ticket

1. Call `POST /api/agent/tickets/draft` with the documented request body from Swagger. Describe a fictional delivery problem and use the existing sample customer/order identifiers.
2. Show the proposed subject and description. Explain that drafting does **not** persist a ticket.
3. Review the proposal, then use the confirmed ticket-creation endpoint, `POST /api/agent/tickets`, with `Confirmed: true` and the fields required by its Swagger schema.
4. Record the new `ticketId` in your private demo notes. Verify via the Business API or `get_ticket` that the ticket exists in SQLite and is open.

### Scene 3 — Draft and submit a resolution proposal

1. Use `POST /api/agent/resolution-requests/draft`:

   ```json
   {
     "ticketId": "REPLACE-WITH-NEW-TICKET-GUID",
     "explanation": "The customer confirmed the delivery issue is resolved and no further assistance is needed."
   }
   ```

2. Review and optionally edit the generated `proposedReason`. The AI draft cannot approve or resolve anything.
3. Call `POST /api/agent/resolution-requests`:

   ```json
   {
     "ticketId": "REPLACE-WITH-NEW-TICKET-GUID",
     "reason": "The customer reports that the delivery issue is resolved and no further assistance is needed.",
     "confirmed": true
   }
   ```

4. Record the returned `approvalId`. Retrieve the approval using `GET /api/approvals/{id}` and verify `Pending`.
5. Retrieve the ticket: it is **still open**. Explain that proposal submission is not resolution.

### Scene 4 — Independent review and state separation

1. Sign into Business API Swagger as a **different reviewer**, if available.
2. Call `POST /api/approvals/{id}/review` using `Tickets.Approval.Review` and the body:

   ```json
   { "decision": "Approved" }
   ```

3. Retrieve the approval: `Approved`.
4. Retrieve the ticket: still `Open` (or its unchanged pre-execution state).
5. Ask an agent: `Check approval APPROVAL-GUID and its ticket. Has the ticket actually been resolved?`
6. Show that the agent reports `Approved` separately from the ticket status; confirm `get_approval` and `get_ticket` were invoked.

**If you lack a second Entra identity:** do not remove the self-review protection. Show the requester endpoint live, then present the existing distinct-user integration tests and clearly label the review/execution segment as test-verified rather than live.

### Scene 5 — Authorized resolution and verification

1. With an authorized caller holding `Tickets.Resolve`, call `POST /api/approvals/{id}/execute`.
2. Retrieve the ticket and confirm status `Resolved`.
3. Ask the agent the same status question again. It should now report an `Approved` approval and a `Resolved` ticket based on successful lookups.
4. Optionally show that a repeat execution attempt returns `409 Conflict`.

### Scene 6 — Optional provider portability comparison

1. Keep the Business API and MCP server configuration unchanged.
2. Set `AI:Provider` to `Ollama`; run one grounded lookup and record its result.
3. Change to `Foundry`, restart only the Agent API, and run the same grounded lookup.
4. Verify tool execution in logs. Discuss that the model output wording can vary while business operations and authorization remain unchanged.
5. Avoid claiming performance or quality superiority from a single prompt comparison. Confirm Azure inference costs are acceptable before running the cloud demo.

## Screenshot plan

Store curated, appropriately redacted images under `docs/screenshots/`. Aim for **four to six** informative screenshots, not every Swagger request.

| Suggested filename | Capture | Caption idea |
|---|---|---|
| `01-agent-grounded-lookup.png` | Agent response and evidence of read-only tool use | Verified customer/order lookup |
| `02-ai-ticket-draft.png` | AI-generated ticket preview | AI assists; user decides |
| `03-pending-resolution-approval.png` | Confirmed proposal's persisted Pending approval | Explicit confirmation creates a review request |
| `04-approved-not-resolved.png` | Approval Approved while ticket remains Open | Approval is not execution |
| `05-resolved-and-verified.png` | Resolved ticket and subsequent agent status report | Agent reports persisted outcome |
| `06-model-provider-switch.png` | Non-sensitive provider setting and equivalent read-only lookup | Ollama and Foundry share the same operations |

Screenshots must not expose access tokens, authorization headers, API keys, client secrets, local user-secret stores, or personal account details. Use fictional customer records and crop unrelated browser UI. Capture only evidence you actually verified.

## Verification checklist

- [ ] Services start and Agent API Swagger authentication works.
- [ ] A read-only AI answer is backed by actual tool invocation.
- [ ] AI ticket draft is separate from confirmed ticket creation.
- [ ] A new ticket persists and can be retrieved.
- [ ] AI resolution draft is separate from confirmed proposal submission.
- [ ] A submitted proposal is Pending while its ticket remains unchanged.
- [ ] Independent review is demonstrated live or accurately represented by integration-test evidence.
- [ ] Approved status is not misreported as Resolved.
- [ ] Authorized execution produces a Resolved ticket.
- [ ] Agent verifies both approval and ticket states through read-only tools.
- [ ] Optional Foundry comparison is correctly labeled and cost-conscious.

## Portfolio talking points

- `IChatClient` makes Ollama and Foundry interchangeable for agent orchestration.
- Native function calling and MCP reuse shared operations without granting model write access.
- OAuth delegated scopes and OBO protect each service boundary independently.
- Business API—not the model prompt—enforces the human approval workflow.
- Persisted approval state and ticket state are intentionally different.
- Scope is deliberately portfolio-focused; comprehensive multitenant access control, concurrency hardening, and production observability are outside this MVP.

See [Architecture](architecture.md) for detailed component, identity, and human-approval diagrams.
