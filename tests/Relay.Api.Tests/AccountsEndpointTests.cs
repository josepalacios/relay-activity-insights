using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Relay.Api.Data;
using Xunit;

namespace Relay.Api.Tests;

public sealed class AccountsEndpointTests : IClassFixture<AccountsEndpointTests.ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;
    public AccountsEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [PostgresFact]
    public async Task Accounts_returns_seeded_accounts_from_postgres()
    {
        using var response = await _client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accounts = json.RootElement.EnumerateArray().ToArray();
        Assert.Equal(20, accounts.Length);
        Assert.All(accounts, account =>
        {
            Assert.Equal(JsonValueKind.Number, account.GetProperty("id").ValueKind);
            Assert.Equal(JsonValueKind.String, account.GetProperty("name").ValueKind);
            Assert.Equal(JsonValueKind.String, account.GetProperty("timeZone").ValueKind);
        });
        Assert.Contains(accounts, account => account.GetProperty("name").GetString() == "Quiet Harbor Spa");
        Assert.All(accounts, account => Assert.Equal(3, account.EnumerateObject().Count()));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(12626, await db.ActivityEvents.CountAsync());
        var quietHarbor = await db.Accounts.SingleAsync(account => account.Name == "Quiet Harbor Spa");
        Assert.Equal(0, await db.ActivityEvents.CountAsync(activity => activity.AccountId == quietHarbor.Id));
    }

    public sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RELAY_TEST_CONNECTION")))
                Skip = "Set RELAY_TEST_CONNECTION to an initialized PostgreSQL database to run this integration test.";
        }
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var connection = Environment.GetEnvironmentVariable("RELAY_TEST_CONNECTION");
            if (!string.IsNullOrWhiteSpace(connection))
                builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["ConnectionStrings:Default"] = connection }));
        }
    }
}
