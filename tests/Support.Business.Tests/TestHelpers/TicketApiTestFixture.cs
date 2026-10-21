using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Support.Business.Api.Data;

namespace Support.Business.Tests.TestHelpers;

public sealed class TicketApiTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AuthorizedBusinessApiFactory _baseFactory;

    public WebApplicationFactory<Program> Factory { get; }

    public TicketApiTestFixture()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _baseFactory = new AuthorizedBusinessApiFactory();
        Factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    DbContextOptions<SupportDbContext>>();

                services.AddDbContext<SupportDbContext>(
                    options => options.UseSqlite(_connection));
            });
        });

        using var scope = Factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<SupportDbContext>();

        db.Database.EnsureCreated();
    }

    public HttpClient CreateClient(string? scope = null, string? objectId = null)
    {
        var client = Factory.CreateClient();

        if (scope is not null)
        {
            client.DefaultRequestHeaders.Add("X-Test-User", "ticket-test-user");
            client.DefaultRequestHeaders.Add("X-Test-Scope", scope);
            client.DefaultRequestHeaders.Add("X-Test-ObjectId", objectId ?? "11111111-1111-4111-8111-111111111111");
        }

        return client;
    }


    public void Dispose()
    {
        Factory.Dispose();
        _baseFactory.Dispose();
        _connection.Dispose();
    }
}
