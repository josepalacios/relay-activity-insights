using Microsoft.EntityFrameworkCore;
using Relay.Api;
using Relay.Api.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddCors(options => options.AddPolicy("LocalDevelopment", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
        .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors("LocalDevelopment");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/accounts", async (AppDbContext db, CancellationToken cancellationToken) =>
    await db.Accounts.AsNoTracking()
        .OrderBy(account => account.Id)
        .Select(account => new { account.Id, account.Name, timeZone = account.Timezone })
        .ToListAsync(cancellationToken));

app.MapGet("/api/accounts/{accountId}/activity-summary", async (
    string accountId, string? weekStart, AppDbContext db, CancellationToken cancellationToken) =>
{
    if (!int.TryParse(accountId, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsedAccountId) || parsedAccountId <= 0)
        return Results.Problem("accountId must be a positive integer.", statusCode: StatusCodes.Status400BadRequest);

    var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == parsedAccountId, cancellationToken);
    if (account is null) return Results.NotFound(new { error = "Account not found." });

    DateOnly requestedWeek;
    if (weekStart is null)
    {
        var reference = new DateOnly(2026, 7, 28);
        requestedWeek = WeeklyActivitySummary.MondayOf(reference).AddDays(-7);
    }
    else if (!DateOnly.TryParseExact(weekStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                 System.Globalization.DateTimeStyles.None, out requestedWeek))
        return Results.Problem("weekStart must be an ISO date in yyyy-MM-dd format.", statusCode: StatusCodes.Status400BadRequest);

    if (requestedWeek.DayOfWeek != DayOfWeek.Monday)
        return Results.Problem("weekStart must be a Monday.", statusCode: StatusCodes.Status400BadRequest);

    TimeZoneInfo zone;
    try { zone = TimeZoneInfo.FindSystemTimeZoneById(account.Timezone); }
    catch (TimeZoneNotFoundException) { return Results.Problem("The account timezone is invalid.", statusCode: StatusCodes.Status500InternalServerError); }
    catch (InvalidTimeZoneException) { return Results.Problem("The account timezone is invalid.", statusCode: StatusCodes.Status500InternalServerError); }

    var latestCompleteWeek = WeeklyActivitySummary.MondayOf(new DateOnly(2026, 7, 28)).AddDays(-7);
    if (requestedWeek > latestCompleteWeek)
        return Results.Problem("weekStart must be a complete week within the supported reporting period.", statusCode: StatusCodes.Status400BadRequest);

    var baselineStarts = Enumerable.Range(1, 4).Select(offset => requestedWeek.AddDays(-7 * offset)).ToArray();
    var allStarts = baselineStarts.Append(requestedWeek).ToArray();
    var fromUtc = WeeklyActivitySummary.ToUtc(allStarts.Min(), zone);
    var toUtc = WeeklyActivitySummary.ToUtc(requestedWeek.AddDays(7), zone);

    var extent = await db.ActivityEvents.AsNoTracking()
        .GroupBy(_ => 1)
        .Select(g => new { Min = g.Min(e => e.OccurredAt), Max = g.Max(e => e.OccurredAt) })
        .SingleOrDefaultAsync(cancellationToken);
    if (extent is null)
        return Results.Problem("No event data is available to establish source coverage.", statusCode: StatusCodes.Status503ServiceUnavailable);

    var rows = await db.ActivityEvents.AsNoTracking()
        .Where(e => e.AccountId == parsedAccountId && e.OccurredAt >= fromUtc && e.OccurredAt < toUtc)
        .Select(e => new { e.Location, e.EventType, e.OccurredAt })
        .ToListAsync(cancellationToken);
    var locations = await db.ActivityEvents.AsNoTracking().Where(e => e.AccountId == parsedAccountId)
        .Select(e => e.Location).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);

    bool IsCovered(DateOnly start) => WeeklyActivitySummary.ToUtc(start, zone) >= extent.Min &&
        WeeklyActivitySummary.ToUtc(start.AddDays(7), zone) <= extent.Max;
    var supported = IsCovered(requestedWeek);
    if (!supported)
        return Results.Problem("weekStart is outside the observed dataset coverage.", statusCode: StatusCodes.Status400BadRequest);

    WeeklyActivitySummary.Counts Count(DateOnly start, string? location = null)
    {
        var startUtc = WeeklyActivitySummary.ToUtc(start, zone);
        var endUtc = WeeklyActivitySummary.ToUtc(start.AddDays(7), zone);
        return WeeklyActivitySummary.Count(rows.Where(x => x.OccurredAt >= startUtc && x.OccurredAt < endUtc &&
            (location is null || x.Location == location)).Select(x => x.EventType));
    }

    var baseline = baselineStarts.Select(start => new
    {
        start = start.ToString("yyyy-MM-dd"), end = start.AddDays(6).ToString("yyyy-MM-dd"),
        total = IsCovered(start) ? (int?)Count(start).Total : null,
        byEventType = IsCovered(start) ? Count(start).ByEventType : null
    }).ToArray();
    var summaryCounts = Count(requestedWeek);
    var coveredBaseline = baseline.Where(x => x.total.HasValue).Select(x => x.total!.Value).ToArray();
    var baselineAverage = coveredBaseline.Length == 4 ? coveredBaseline.Average() : (double?)null;
    double? percentageChange = baselineAverage is > 0 ? (summaryCounts.Total - baselineAverage.Value) / baselineAverage.Value * 100 : null;

    var locationSummaries = locations.Select(location =>
    {
        var current = Count(requestedWeek, location);
        return new
        {
            name = location, total = current.Total, byEventType = current.ByEventType,
            baseline = new { weeklyTotals = baselineStarts.Select(start => IsCovered(start) ? (int?)Count(start, location).Total : null).ToArray() }
        };
    }).ToArray();

    return Results.Ok(new
    {
        account = new { account.Id, account.Name, timeZone = account.Timezone },
        reportingWeek = new { start = requestedWeek.ToString("yyyy-MM-dd"), end = requestedWeek.AddDays(6).ToString("yyyy-MM-dd") },
        baselineWeeks = baseline,
        coverage = new { status = coveredBaseline.Length == 4 ? "complete" : "insufficient", baselineWeeksRequired = 4, baselineWeeksAvailable = coveredBaseline.Length },
        summary = new { total = summaryCounts.Total, baselineAverage, percentageChange, byEventType = summaryCounts.ByEventType },
        locations = locationSummaries
    });
});

if (args.Contains("--initialize-database", StringComparer.Ordinal))
{
    await InitializeDatabaseAsync(app.Services, app.Environment.ContentRootPath);
    return;
}

app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services, string contentRoot)
{
    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    await using var connection = (NpgsqlConnection)db.Database.GetDbConnection();
    await connection.OpenAsync();
    await using (var utc = new NpgsqlCommand("SET TIME ZONE 'UTC'", connection))
        await utc.ExecuteNonQueryAsync();

    await using var transaction = await connection.BeginTransactionAsync();
    await using (var seedLock = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtext('relay-seed-initialization'))", connection, transaction))
        await seedLock.ExecuteNonQueryAsync();
    await using var counts = new NpgsqlCommand("SELECT (SELECT COUNT(*) FROM accounts), (SELECT COUNT(*) FROM activity_events)", connection, transaction);
    await using var reader = await counts.ExecuteReaderAsync();
    await reader.ReadAsync();
    var accountCount = reader.GetInt64(0);
    var eventCount = reader.GetInt64(1);
    await reader.DisposeAsync();

    if (accountCount == 20 && eventCount == 12626)
    {
        await transaction.RollbackAsync();
        Console.WriteLine("Database is already initialized with 20 accounts and 12626 activity events.");
        return;
    }

    if (accountCount != 0 || eventCount != 0)
        throw new InvalidOperationException($"Refusing to seed a partial or unexpected database ({accountCount} accounts, {eventCount} events). Inspect it manually; no records were changed.");

    var seedPath = Path.Combine(contentRoot, "seed.sql");
    if (!File.Exists(seedPath)) seedPath = Path.Combine(AppContext.BaseDirectory, "seed.sql");
    var seedSql = await File.ReadAllTextAsync(seedPath);
    await using var seed = new NpgsqlCommand(seedSql, connection, transaction) { CommandTimeout = 120 };
    await seed.ExecuteNonQueryAsync();
    await transaction.CommitAsync();
    Console.WriteLine("Database initialized with the unchanged seed.sql (20 accounts, 12626 activity events).");
}

public partial class Program;
