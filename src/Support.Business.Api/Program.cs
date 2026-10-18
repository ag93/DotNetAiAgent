using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Support.Business.Api.Services;

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
        var scope = $"api://{businessApiClientId}/Support.Read";

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
                            [scope] = "Read support records"
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
});

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

app.Run();

public partial class Program { }