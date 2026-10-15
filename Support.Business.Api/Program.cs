using Support.Business.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<CustomerService>();
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

        if (customer is null)
            return Results.NotFound();

        return Results.Ok(customer);
    });

app.Run();

public partial class Program { }