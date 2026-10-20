using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Support.Business.Api.Data;
using Support.Business.Api.Data.Entities;
using Support.Business.Api.Services;
using Support.Business.Api.Tickets;
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
                            [ticketsCreateScope] = "Create support tickets"
                        }
                    }
                }
            });

        options.AddSecurityRequirement(document =>
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    "EntraOAuth", document)] = [supportReadScope, ticketsCreateScope]
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


app.Run();

public partial class Program { }