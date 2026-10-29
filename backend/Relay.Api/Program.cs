using Microsoft.EntityFrameworkCore;
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
