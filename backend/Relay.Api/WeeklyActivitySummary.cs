namespace Relay.Api;

/// <summary>Calendar and deterministic count rules shared by the reporting endpoint and focused tests.</summary>
public static class WeeklyActivitySummary
{
    public static readonly string[] EventTypes = ["call_received", "lead_created", "appointment_set"];

    public static DateOnly MondayOf(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public static DateTime ToUtc(DateOnly localDate, TimeZoneInfo timeZone)
    {
        var localMidnight = DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localMidnight, timeZone);
    }

    public static Counts Count(IEnumerable<string> eventTypes)
    {
        var observed = eventTypes.GroupBy(x => x, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var byType = EventTypes.ToDictionary(type => type, type => observed.GetValueOrDefault(type), StringComparer.Ordinal);
        return new Counts(byType.Values.Sum(), byType);
    }

    public sealed record Counts(int Total, IReadOnlyDictionary<string, int> ByEventType);
}
