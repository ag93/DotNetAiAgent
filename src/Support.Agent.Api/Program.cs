using Microsoft.Extensions.AI;
using OllamaSharp;
using Support.Agent.Api.Agents;
using Support.Agent.Api.Services;
using Support.Agent.Api.Tools;

var builder = WebApplication.CreateBuilder(args);
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
builder.Services.AddScoped<CustomerAgent>();
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
        });



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
        });


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
    });


if (app.Environment.IsDevelopment())
{
    app.MapPost(
        "/api/agent/customer-chat",
        async (
            CustomerChatRequest request, CustomerAgent agent, CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return Results.BadRequest("A message is required.");
                }

                var result = await agent.AskAsync(request.Message, cancellationToken);

                return Results.Ok(result);
            });
}


app.Run();

public partial class Program { }

public sealed record ChatTestRequest(string Message);
public sealed record ChatTestResponse(string? Reply);
public sealed record CustomerChatRequest(string Message);