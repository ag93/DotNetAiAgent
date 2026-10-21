using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using ModelContextProtocol.AspNetCore;
using Support.Mcp.Server.Security;
using Support.Mcp.Server.Tools;
using Support.Operations.Services;
using Support.Operations.Tools;
using Support.Mcp.Server.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("McpInvoke", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireScope("Mcp.Invoke");
    });
});

var businessApiBaseUrl = builder.Configuration["BusinessApi:BaseUrl"]
            ?? throw new InvalidOperationException("BusinessApi:BaseUrl is not configured.");
var businessApiBaseUri = new Uri(businessApiBaseUrl);

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BusinessApiOboHandler>();
builder.Services.AddHttpClient<BusinessApiCustomerClient>(
    client =>
    {
        client.BaseAddress = businessApiBaseUri;
    })
    .AddHttpMessageHandler<BusinessApiOboHandler>()
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

builder.Services.AddHttpClient<BusinessApiOrderClient>(
    client =>
    {
        client.BaseAddress = businessApiBaseUri;
    })
    .AddHttpMessageHandler<BusinessApiOboHandler>()
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

builder.Services
    .AddHttpClient<BusinessApiTicketClient>(client =>
    {
        client.BaseAddress = businessApiBaseUri;
    })
    .AddHttpMessageHandler<BusinessApiOboHandler>()
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

builder.Services
    .AddHttpClient<BusinessApiApprovalClient>(client =>
    {
        client.BaseAddress = businessApiBaseUri;
    })
    .AddHttpMessageHandler<BusinessApiOboHandler>()
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

builder.Services.AddScoped<CustomerLookupTool>();
builder.Services.AddScoped<OrderLookupTool>();
builder.Services.AddScoped<TicketLookupTool>();
builder.Services.AddTransient<ApprovalLookupTool>();


builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.SessionMode = HttpServerSessionMode.Stateless;
    })
    .WithTools<ServerInfoTool>()
    .WithTools<CustomerMcpTool>()
    .WithTools<OrderMcpTool>()
    .WithTools<TicketMcpTool>()
    .WithTools<ApprovalMcpTool>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DownstreamHttpExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapMcp("/mcp").RequireAuthorization("McpInvoke");

app.Run();

public partial class Program { }  
