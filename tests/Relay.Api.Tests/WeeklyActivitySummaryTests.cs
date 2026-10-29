using Relay.Api;
using Xunit;

namespace Relay.Api.Tests;

public sealed class WeeklyActivitySummaryTests
{
    [Theory]
    [InlineData(2026, 7, 20, 2026, 7, 20)]
    [InlineData(2026, 7, 26, 2026, 7, 20)]
    [InlineData(2026, 7, 27, 2026, 7, 27)]
    public void Monday_of_returns_local_week_start(int y, int m, int d, int expectedY, int expectedM, int expectedD) =>
        Assert.Equal(new DateOnly(expectedY, expectedM, expectedD), WeeklyActivitySummary.MondayOf(new DateOnly(y, m, d)));

    [Fact]
    public void Chicago_week_boundaries_are_half_open_and_respect_dst()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        Assert.Equal(new DateTime(2026, 3, 2, 6, 0, 0, DateTimeKind.Utc), WeeklyActivitySummary.ToUtc(new DateOnly(2026, 3, 2), zone));
        Assert.Equal(new DateTime(2026, 3, 9, 5, 0, 0, DateTimeKind.Utc), WeeklyActivitySummary.ToUtc(new DateOnly(2026, 3, 9), zone));
        Assert.Equal(TimeSpan.FromHours(167), WeeklyActivitySummary.ToUtc(new DateOnly(2026, 3, 9), zone) - WeeklyActivitySummary.ToUtc(new DateOnly(2026, 3, 2), zone));
    }

    [Fact]
    public void Count_zero_fills_supported_event_types_and_sums_to_total()
    {
        var counts = WeeklyActivitySummary.Count(["call_received", "call_received", "lead_created"]);
        Assert.Equal(3, counts.Total);
        Assert.Equal(2, counts.ByEventType["call_received"]);
        Assert.Equal(1, counts.ByEventType["lead_created"]);
        Assert.Equal(0, counts.ByEventType["appointment_set"]);
        Assert.Equal(0, WeeklyActivitySummary.Count([]).Total);
    }
}
