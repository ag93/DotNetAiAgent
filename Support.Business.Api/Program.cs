using Support.Business.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<CustomerService>();
builder.Services.AddSingleton<OrderService>();
var app = builder.Build();

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
    });


app.MapGet(
    "/api/orders/{orderId}",
    (string orderId, OrderService orderService) =>
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return Results.BadRequest();

        var order = orderService.GetById(orderId);
        return order is null ? Results.NotFound() : Results.Ok(order);
    });

app.Run();

public partial class Program { }