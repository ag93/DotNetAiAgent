using Microsoft.Extensions.AI;
using OllamaSharp;
using Support.Agent.Api.Agents;
using Support.Operations.Services;
using Support.Operations.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AgentInvoke", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireScope("Agent.Invoke");
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<BusinessApiCustomerClient>(
    client =>
    {
        var baseUrl = builder.Configuration["BusinessApi:BaseUrl"]
            ?? throw new InvalidOperationException("BusinessApi:BaseUrl is not configured.");
        client.BaseAddress = new Uri(baseUrl);
    });

builder.Services.AddHttpClient<BusinessApiOrderClient>(
    client =>
    {
        var baseUrl = builder.Configuration["BusinessApi:BaseUrl"]
            ?? throw new InvalidOperationException(
                "BusinessApi:BaseUrl is not configured.");

        client.BaseAddress = new Uri(baseUrl);
    });
builder.Services.AddScoped<CustomerLookupTool>();
builder.Services.AddScoped<OrderLookupTool>();
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

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
if (builder.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
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
        async (
            SupportChatRequest request, McpSupportAgent agent, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest("A message is required.");
            }

            var result = await agent.AskAsync(request.Message, cancellationToken);

            return Results.Ok(result);
        }).RequireAuthorization("AgentInvoke");
}



app.Run();

public partial class Program { }

public sealed record ChatTestRequest(string Message);
public sealed record ChatTestResponse(string? Reply);
public sealed record SupportChatRequest(string Message);