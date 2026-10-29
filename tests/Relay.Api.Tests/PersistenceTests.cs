using Microsoft.EntityFrameworkCore;
using Relay.Api.Data;
using Xunit;

namespace Relay.Api.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public void Model_matches_schema_names_lengths_relations_and_utc_storage()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=relay;Username=relay;Password=relay")
            .Options;
        using var db = new AppDbContext(options);
        var account = db.Model.FindEntityType(typeof(Account))!;
        var activity = db.Model.FindEntityType(typeof(ActivityEvent))!;

        Assert.Equal("accounts", account.GetTableName());
        Assert.Equal("activity_events", activity.GetTableName());
        Assert.Equal("timestamp with time zone", activity.FindProperty(nameof(ActivityEvent.OccurredAt))!.GetColumnType());
        Assert.Equal("timestamp with time zone", account.FindProperty(nameof(Account.CreatedAt))!.GetColumnType());
        Assert.Equal(120, account.FindProperty(nameof(Account.Name))!.GetMaxLength());
        Assert.Equal(80, activity.FindProperty(nameof(ActivityEvent.Location))!.GetMaxLength());
        Assert.Contains(activity.GetForeignKeys(), foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Account));
        Assert.Contains(activity.GetIndexes(), index => index.Properties.Select(property => property.Name)
            .SequenceEqual([nameof(ActivityEvent.AccountId), nameof(ActivityEvent.OccurredAt)]));
    }
}
