using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Support.Business.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
    app.UseSwaggerUI();
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