namespace Relay.Api.Data;

public sealed class Account
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Industry { get; set; }
    public required string Timezone { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ActivityEvent> ActivityEvents { get; set; } = new List<ActivityEvent>();
}
