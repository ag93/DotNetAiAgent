using Support.Agent.Api.Services;

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
builder.Services.AddOpenApi();

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


app.Run();