namespace Relay.Api.Data;

public sealed class ActivityEvent
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public required string Location { get; set; }
    public required string EventType { get; set; }
    public DateTime OccurredAt { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Outcome { get; set; }
    public Account Account { get; set; } = null!;
}
