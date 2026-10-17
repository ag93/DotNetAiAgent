using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using Support.Mcp.Server.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.SessionMode =
            HttpServerSessionMode.Stateless;
    })
    .WithTools<ServerInfoTool>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapMcp("/mcp");

app.Run();
