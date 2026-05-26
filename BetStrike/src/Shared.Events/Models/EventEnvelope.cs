using System;

namespace Shared.Events.Models;

public class EventEnvelope<T>
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public T Payload { get; set; } = default!;
}
