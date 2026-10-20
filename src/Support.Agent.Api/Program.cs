using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using OllamaSharp;
using Support.Agent.Api.Agents;
using Support.Agent.Api.ErrorHandling;
using Support.Agent.Api.Requests;
using Support.Agent.Api.Security;
using Support.Contracts.Tickets;
using Support.Operations.Services;
using Support.Operations.Tools;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BusinessApiOboHandler>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AgentInvoke", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireScope("Agent.Invoke");
    });
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    var tenantId = builder.Configuration["AzureAd:TenantId"];
    var agentApiClientId = builder.Configuration["AzureAd:ClientId"];

    if (builder.Environment.IsDevelopment() 
        & !string.IsNullOrWhiteSpace(tenantId) 
        && !string.IsNullOrWhiteSpace(agentApiClientId))
    {
        var scope =
            $"api://{agentApiClientId}/Agent.Invoke";

        options.AddSecurityDefinition(
            "EntraOAuth",
            new OpenApiSecurityScheme
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
                            [scope] = "Invoke the support agent"
                        }
                    }
                }
            });

        options.AddSecurityRequirement(document =>
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    "EntraOAuth", document)] = [scope]
            });
    }
});

var businessApiBaseUrl = builder.Configuration["BusinessApi:BaseUrl"]
    ?? throw new InvalidOperationException("BusinessApi:BaseUrl is not configured.");

builder.Services.AddHttpClient<BusinessApiCustomerClient>(
    client =>
    {
        client.BaseAddress = new Uri(businessApiBaseUrl);
    })
    .AddHttpMessageHandler<BusinessApiOboHandler>();

builder.Services.AddHttpClient<BusinessApiOrderClient>(
    client =>
    {
        client.BaseAddress = new Uri(businessApiBaseUrl);
    })
    .AddHttpMessageHandler<BusinessApiOboHandler>();

builder.Services.AddHttpClient<BusinessApiTicketClient>(
    client =>
    {
        client.BaseAddress = new Uri(businessApiBaseUrl);
    })
    .AddHttpMessageHandler<BusinessApiOboHandler>();

builder.Services.AddHttpClient("BusinessTicketWrites", 
    client =>
    {
        client.BaseAddress = new Uri(businessApiBaseUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            AllowAutoRedirect = false
        });


builder.Services.AddScoped<CustomerLookupTool>();
builder.Services.AddScoped<OrderLookupTool>();
builder.Services.AddScoped<TicketLookupTool>();
builder.Services.AddScoped<SupportAgent>();
builder.Services.AddScoped<McpSupportAgent>();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IChatClient>(_ =>
{
    var baseUrl = builder.Configuration["Ollama:BaseUrl"]
        ?? throw new InvalidOperationException("Ollama:BaseUrl is not configured.");

    var model = builder.Configuration["Ollama:Model"]
        ?? throw new InvalidOperationException("Ollama:Model is not configured.");

    return new ChatClientBuilder(
        new OllamaApiClient(new Uri(baseUrl), model))
        .UseFunctionInvocation(
            configure: invoker =>
            {
                invoker.MaximumIterationsPerRequest = 3;
            })
        .Build();
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DownstreamHttpExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
if (builder.Environment.IsDevelopment())
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
            options.OAuthAppName("DotNetAiAgent Swagger");
        }
    });

}


app.MapGet(
    "/api/agent/customers/{customerId}",
    async (
        string customerId,
        BusinessApiCustomerClient customerClient,
        CancellationToken cancellationToken) =>
        {
            try
            {
                var customer = await customerClient.GetByIdAsync(customerId, cancellationToken);

                return customer is null
                    ? Results.NotFound()
                    : Results.Ok(customer);
            }
            catch (HttpRequestException)
            {
                return Results.Problem(
                    title: "Business API request failed.",
                    statusCode: StatusCodes.Status502BadGateway);
            }
        }).RequireAuthorization("AgentInvoke");



app.MapGet(
    "/api/agent/orders/{orderId}",
    async (
        string orderId,
        BusinessApiOrderClient orderClient,
        CancellationToken cancellationToken) =>
        {
            try
            {
                var order = await orderClient.GetByIdAsync(orderId,  cancellationToken);

                return order is null
                    ? Results.NotFound()
                    : Results.Ok(order);
            }
            catch (HttpRequestException)
            {
                return Results.Problem(
                    title: "Business API request failed.",
                    statusCode: StatusCodes.Status502BadGateway);
            }
        }).RequireAuthorization("AgentInvoke");


app.MapPost(
    "/api/agent/chat-test",
    async (
        ChatTestRequest request,
        IChatClient chatClient,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest(
                "A message is required.");
        }

        var response = await chatClient.GetResponseAsync(
            request.Message,
            cancellationToken: cancellationToken);

        return Results.Ok(new ChatTestResponse(response.Text));
    }).RequireAuthorization("AgentInvoke");


if (app.Environment.IsDevelopment())
{
    app.MapPost(
        "/api/agent/customer-chat",
        async (
            SupportChatRequest request, SupportAgent agent, CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return Results.BadRequest("A message is required.");
                }

                var result = await agent.AskAsync(request.Message, cancellationToken);

                return Results.Ok(result);
            }).RequireAuthorization("AgentInvoke");
}


if (app.Environment.IsDevelopment())
{

    app.MapPost(
        "/api/agent/mcp-chat",
        async (SupportChatRequest request, McpSupportAgent agent,
            ITokenAcquisition tokenAcquisition, IConfiguration configuration,
            HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest("A message is required.");
            }

            var scope = configuration["McpServer:Scope"];

            if (string.IsNullOrWhiteSpace(scope))
            {
                throw new InvalidOperationException("McpServer:Scope is not configured.");
            }

            var mcpAccessToken = await tokenAcquisition.GetAccessTokenForUserAsync([scope], user: httpContext.User);
            var result = await agent.AskAsync(request.Message, mcpAccessToken, cancellationToken);

            return Results.Ok(result);
        })
        .RequireAuthorization("AgentInvoke");

}

if (app.Environment.IsDevelopment())
{

    app.MapPost("/api/agent/tickets", async (
        ConfirmedTicketCreateRequest request,
        ITokenAcquisition tokenAcquisition,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        var ticket = request.Ticket;

        if (ticket is null ||
            string.IsNullOrWhiteSpace(ticket.CustomerId) ||
            ticket.CustomerId.Trim().Length > 40 ||
            (ticket.OrderId?.Trim().Length ?? 0) > 40 ||
            string.IsNullOrWhiteSpace(ticket.Subject) ||
            ticket.Subject.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(ticket.Description) ||
            ticket.Description.Trim().Length > 4000)
        {
            return Results.BadRequest(new
            {
                error = "Invalid ticket details."
            });
        }

        var normalizedTicket = new CreateTicketRequest(
            ticket.CustomerId.Trim(),
            string.IsNullOrWhiteSpace(ticket.OrderId) ? null : ticket.OrderId.Trim(),
            ticket.Subject.Trim(),
            ticket.Description.Trim());

        // Preview only. No token acquisition or database write.
        if (!request.Confirmed)
        {
            return Results.Ok(new
            {
                requiresConfirmation = true,
                proposedTicket = normalizedTicket,
                message = "Review the details and explicitly confirm creation."
            });
        }

        var scope = configuration["BusinessApi:CreateTicketScope"];

        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new InvalidOperationException(
                "BusinessApi:CreateTicketScope is not configured.");
        }

        var accessToken =
            await tokenAcquisition.GetAccessTokenForUserAsync(
                [scope],
                user: httpContext.User);

        var client =
            httpClientFactory.CreateClient("BusinessTicketWrites");

        using var outboundRequest =
            new HttpRequestMessage(HttpMethod.Post, "api/tickets")
            {
                Content = JsonContent.Create(normalizedTicket)
            };

        outboundRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using var response = await client.SendAsync(
            outboundRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var created = await response.Content
            .ReadFromJsonAsync<TicketResponse>(
                cancellationToken);

        if (created is null)
        {
            throw new HttpRequestException(
                "The downstream service returned an empty ticket response.");
        }

        return Results.Ok(new
        {
            created = true,
            ticket = created
        });
    })
    .RequireAuthorization("AgentInvoke");
}


app.Run();

public partial class Program { }

public sealed record ChatTestResponse(string? Reply);