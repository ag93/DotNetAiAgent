using ModelContextProtocol.AspNetCore;
using Support.Mcp.Server.Tools;
using Support.Operations.Services;
using Support.Operations.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<BusinessApiCustomerClient>(
    client =>
    {
        var baseUrl = builder.Configuration["BusinessApi:BaseUrl"]
            ?? throw new InvalidOperationException(
                "BusinessApi:BaseUrl is not configured.");

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


builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.SessionMode = HttpServerSessionMode.Stateless;
    })
    .WithTools<ServerInfoTool>()
    .WithTools<CustomerMcpTool>()
    .WithTools<OrderMcpTool>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapMcp("/mcp");

app.Run();
