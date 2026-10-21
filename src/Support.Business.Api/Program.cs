using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Support.Business.Api.Approvals;
using Support.Business.Api.Data;
using Support.Business.Api.Data.Entities;
using Support.Business.Api.Security;
using Support.Business.Api.Services;
using Support.Business.Api.Tickets;
using Support.Contracts.Approvals;
using Support.Contracts.Tickets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    var tenantId = builder.Configuration["AzureAd:TenantId"];
    var businessApiClientId =
        builder.Configuration["AzureAd:ClientId"];

    if (builder.Environment.IsDevelopment()
        && !string.IsNullOrWhiteSpace(tenantId)
        && !string.IsNullOrWhiteSpace(businessApiClientId))
    {
        var supportReadScope = $"api://{businessApiClientId}/Support.Read";
        var ticketsCreateScope = $"api://{businessApiClientId}/Tickets.Create";
        var ticketsApprovalRequestScope = $"api://{businessApiClientId}/Tickets.Approval.Request";
        var ticketsApprovalReviewScope = $"api://{businessApiClientId}/Tickets.Approval.Review";

        options.AddSecurityDefinition("EntraOAuth", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"),

                        TokenUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"),

                        Scopes = new Dictionary<string, string>
                        {
                            [supportReadScope] = "Read support records",
                            [ticketsCreateScope] = "Create support tickets",
                            [ticketsApprovalRequestScope] = "Submit ticket resolution approval requests",
                            [ticketsApprovalReviewScope] = "Review pending ticket resolution requests"
                        }
                    }
                }
            });

        options.AddSecurityRequirement(document =>
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("EntraOAuth", document)] 
                    =  [supportReadScope, ticketsCreateScope, ticketsApprovalReviewScope, ticketsApprovalRequestScope]
            });
    }
});

builder.Services.AddSingleton<CustomerService>();
builder.Services.AddSingleton<OrderService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SupportRead", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireScope("Support.Read");
    });

    options.AddPolicy("TicketsCreate", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireScope("Tickets.Create");
    });

    options.AddPolicy("TicketsApprovalRequest", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireScope("Tickets.Approval.Request");
    });

    options.AddPolicy("TicketsApprovalReview", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireScope("Tickets.Approval.Review");
    });

});


var databaseDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(databaseDirectory);
var databasePath = Path.Combine(databaseDirectory, "support-tickets.db");
builder.Services.AddDbContext<SupportDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var swaggerClientId = builder.Configuration["SwaggerOAuth:ClientId"];

        if (!string.IsNullOrWhiteSpace(swaggerClientId))
        {
            options.OAuthClientId(swaggerClientId);
            options.OAuthUsePkce();
            options.OAuthAppName("DotNetAiAgent Business API Swagger");
        }
    });

}

app.UseHttpsRedirection();

app.MapGet(
    "/api/customers/{customerId}",
    (string customerId, CustomerService customerService) =>
    {
        if(string.IsNullOrWhiteSpace(customerId))
            return Results.BadRequest();

        var customer = customerService.GetById(customerId);
        return customer is null ? Results.NotFound() : Results.Ok(customer);
    })
    .RequireAuthorization("SupportRead");


app.MapGet(
    "/api/orders/{orderId}",
    (string orderId, OrderService orderService) =>
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return Results.BadRequest();

        var order = orderService.GetById(orderId);
        return order is null ? Results.NotFound() : Results.Ok(order);
    })
    .RequireAuthorization("SupportRead");


app.MapPost("/api/tickets", async (
    CreateTicketRequest request,
    SupportDbContext db,
    CancellationToken cancellationToken) =>
{
    var customerId = request.CustomerId?.Trim();
    var orderId = request.OrderId?.Trim();
    var subject = request.Subject?.Trim();
    var description = request.Description?.Trim();

    if (string.IsNullOrWhiteSpace(customerId) ||
        customerId.Length > 40 ||
        string.IsNullOrWhiteSpace(subject) ||
        subject.Length > 200 ||
        string.IsNullOrWhiteSpace(description) ||
        description.Length > 4000 ||
        orderId?.Length > 40)
    {
        return Results.BadRequest(new
        {
            error = "Invalid ticket data. CustomerId, Subject and Description " +
                    "are required and must be within their maximum lengths."
        });
    }

    if (string.IsNullOrWhiteSpace(orderId))
    {
        orderId = null;
    }

    var now = DateTimeOffset.UtcNow;

    var ticket = new SupportTicket
    {
        CustomerId = customerId,
        OrderId = orderId,
        Subject = subject,
        Description = description,
        Status = TicketStatus.Open,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    db.Tickets.Add(ticket);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/tickets/{ticket.Id}", ticket.ToResponse());
})
.RequireAuthorization("TicketsCreate");


app.MapGet("/api/tickets/{id:guid}", async (
    Guid id,
    SupportDbContext db,
    CancellationToken cancellationToken) =>
{
    var ticket = await db.Tickets
        .AsNoTracking()
        .FirstOrDefaultAsync(
            t => t.Id == id,
            cancellationToken);

    return ticket is null
        ? Results.NotFound()
        : Results.Ok(ticket.ToResponse());
})
.RequireAuthorization("SupportRead");


app.MapPost(
    "/api/tickets/{ticketId:guid}/resolution-requests",
    async (
        Guid ticketId,
        CreateResolutionApprovalRequest request,
        SupportDbContext db,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        var reason = request.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
        {
            return Results.BadRequest(new
            {
                error = "A resolution reason of at most 1000 characters is required."
            });
        }

        var ticket = await db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == ticketId,
                cancellationToken);

        if (ticket is null)
        {
            return Results.NotFound(new
            {
                error = "Ticket not found."
            });
        }

        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            return Results.Conflict(new
            {
                error = "A resolved or closed ticket cannot be proposed for resolution."
            });
        }

        var alreadyPending =
            await db.TicketApprovalRequests
                .AnyAsync(
                    a => a.TicketId == ticketId &&
                         a.Status == ApprovalStatus.Pending,
                    cancellationToken);

        if (alreadyPending)
        {
            return Results.Conflict(new
            {
                error = "This ticket already has a pending resolution request."
            });
        }

        var requesterId = ApprovalUserIdentity.GetObjectId(httpContext.User);

        if (requesterId is null)
        {
            return Results.Forbid();
        }

        var approval = new TicketApprovalRequest
        {
            TicketId = ticketId,
            Reason = reason,
            Status = ApprovalStatus.Pending,
            RequestedAtUtc = DateTimeOffset.UtcNow,
            ReviewedAtUtc = null,
            ReviewedBy = null,
            RequestedBy = requesterId
        };

        db.TicketApprovalRequests.Add(approval);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/approvals/{approval.Id}",
            approval.ToResponse());
    })
    .RequireAuthorization("TicketsApprovalRequest");


app.MapGet("/api/approvals/{id:guid}", async (
    Guid id,
    SupportDbContext db,
    CancellationToken cancellationToken) =>
{
    var approval = await db.TicketApprovalRequests
        .AsNoTracking()
        .FirstOrDefaultAsync(
            a => a.Id == id,
            cancellationToken);

    return approval is null
        ? Results.NotFound()
        : Results.Ok(approval.ToResponse());
})
.RequireAuthorization(["SupportRead"]);


app.MapPost("/api/approvals/{id:guid}/review", async (
    Guid id,
    ReviewResolutionApprovalRequest request,
    SupportDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var decision = request.Decision?.Trim();

    if (decision is not ("Approved" or "Rejected"))
    {
        return Results.BadRequest(new
        {
            error = "Decision must be Approved or Rejected."
        });
    }

    var approval = await db.TicketApprovalRequests
        .FirstOrDefaultAsync(
            a => a.Id == id,
            cancellationToken);

    if (approval is null)
    {
        return Results.NotFound();
    }

    if (approval.Status != ApprovalStatus.Pending)
    {
        return Results.Conflict(new
        {
            error = "This approval request has already been reviewed."
        });
    }

    if (string.IsNullOrWhiteSpace(approval.RequestedBy))
    {
        return Results.Conflict(new
        {
            error =
                "This request predates requester identity tracking. " +
                "Submit a new resolution request."
        });
    }

    var reviewerId =
        ApprovalUserIdentity.GetObjectId(httpContext.User);

    if (reviewerId is null)
    {
        return Results.Forbid();
    }

    if (string.Equals(
        approval.RequestedBy,
        reviewerId,
        StringComparison.OrdinalIgnoreCase))
    {
        return Results.Forbid();
    }

    approval.Status = decision == "Approved"
        ? ApprovalStatus.Approved
        : ApprovalStatus.Rejected;

    approval.ReviewedAtUtc = DateTimeOffset.UtcNow;
    approval.ReviewedBy = reviewerId;

    await db.SaveChangesAsync(cancellationToken);

    return Results.Ok(approval.ToResponse());
})
.RequireAuthorization("TicketsApprovalReview");


app.Run();

public partial class Program { }