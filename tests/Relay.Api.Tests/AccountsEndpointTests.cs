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

    [PostgresFact]
    public async Task Activity_summary_returns_summit_seed_aggregation_and_empty_account()
    {
        using var response = await _client.GetAsync("/api/accounts/1/activity-summary?weekStart=2026-07-20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal("Summit Auto Group", root.GetProperty("account").GetProperty("name").GetString());
        Assert.Equal("America/Chicago", root.GetProperty("account").GetProperty("timeZone").GetString());
        Assert.Equal("2026-07-20", root.GetProperty("reportingWeek").GetProperty("start").GetString());
        Assert.Equal(53, root.GetProperty("summary").GetProperty("total").GetInt32());
        Assert.Equal(48.5, root.GetProperty("summary").GetProperty("baselineAverage").GetDouble());
        Assert.Equal(new[] { 50, 43, 62, 39 }, root.GetProperty("baselineWeeks").EnumerateArray()
            .Select(x => x.GetProperty("total").GetInt32()).ToArray());
        Assert.Equal(6, root.GetProperty("locations").GetArrayLength());
        foreach (var week in root.GetProperty("baselineWeeks").EnumerateArray())
        {
            var breakdown = week.GetProperty("byEventType");
            Assert.Equal(week.GetProperty("total").GetInt32(), breakdown.EnumerateObject().Sum(x => x.Value.GetInt32()));
        }
        var current = root.GetProperty("summary");
        Assert.Equal(current.GetProperty("total").GetInt32(), current.GetProperty("byEventType").EnumerateObject().Sum(x => x.Value.GetInt32()));

        using var empty = await _client.GetAsync("/api/accounts/20/activity-summary?weekStart=2026-07-20");
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        using var emptyJson = JsonDocument.Parse(await empty.Content.ReadAsStringAsync());
        Assert.Equal(0, emptyJson.RootElement.GetProperty("summary").GetProperty("total").GetInt32());
        Assert.Equal("complete", emptyJson.RootElement.GetProperty("coverage").GetProperty("status").GetString());
        Assert.Empty(emptyJson.RootElement.GetProperty("locations").EnumerateArray());

        using var locationWeek = await _client.GetAsync("/api/accounts/6/activity-summary?weekStart=2026-02-23");
        Assert.Equal(HttpStatusCode.OK, locationWeek.StatusCode);
        using var locationJson = JsonDocument.Parse(await locationWeek.Content.ReadAsStringAsync());
        var knownZeroLocation = locationJson.RootElement.GetProperty("locations").EnumerateArray()
            .Single(x => x.GetProperty("name").GetString() == "Site N");
        Assert.Equal(0, knownZeroLocation.GetProperty("total").GetInt32());
        Assert.All(knownZeroLocation.GetProperty("byEventType").EnumerateObject(), item => Assert.Equal(0, item.Value.GetInt32()));
    }

    [PostgresTheory]
    [InlineData("2026-07-21")]
    [InlineData("not-a-date")]
    [InlineData("2026-08-03")]
    public async Task Activity_summary_rejects_invalid_or_unsupported_week(string week)
    {
        using var response = await _client.GetAsync($"/api/accounts/1/activity-summary?weekStart={week}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [PostgresFact]
    public async Task Activity_summary_returns_not_found_for_missing_account()
    {
        using var response = await _client.GetAsync("/api/accounts/9999/activity-summary?weekStart=2026-07-20");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Activity_summary_rejects_invalid_account_id(string accountId)
    {
        using var response = await _client.GetAsync($"/api/accounts/{accountId}/activity-summary?weekStart=2026-07-20");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RELAY_TEST_CONNECTION")))
                Skip = "Set RELAY_TEST_CONNECTION to an initialized PostgreSQL database to run this integration test.";
        }
    }

    public sealed class PostgresTheoryAttribute : TheoryAttribute
    {
        public PostgresTheoryAttribute()
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
