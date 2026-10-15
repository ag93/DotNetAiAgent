using Support.Business.Api.Customers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var customers = new Dictionary<string, CustomerResponse>(StringComparer.OrdinalIgnoreCase)
{
    ["C-1002"] = new("C-1002", "Alex Morgan", "alex@example.com"),
    ["C-1003"] = new("C-1003", "Jordan Lee", "jordan@example.com")
};

app.MapGet(
    "/api/customers/{customerId}",
    (string customerId) =>
    {
        if(string.IsNullOrWhiteSpace(customerId))
            return Results.BadRequest();

        if(!customers.TryGetValue(customerId, out var customer))
            return Results.NotFound();

        return Results.Ok(customer);
    });

app.Run();