using System.Text.Json;

namespace FtrackDotNet.EventHub;

internal class FtrackEventEnvelope
{
    public FtrackEvent[] Args { get; set; } = null!;
    public string? Name { get; set; }
}

/// <summary>
/// Represents an event structure coming from Ftrack's event hub.
/// </summary>
public class FtrackEvent
{
    public string? Topic { get; set; }
    public object? Data { get; set; }
    public string Target { get; set; } = string.Empty;
    public string? InReplyToEvent { get; set; }
    public string? Id { get; set; } = Guid.NewGuid().ToString();
    public FtrackEventSource? Source { get; set; }
}

public class FtrackEventSource
{
    public string ClientToken { get; set; } = null!;
    public string? ApplicationId { get; set; }
    public FtrackEventSourceUser? User { get; set; }
    public string? Id { get; set; }
}

public class FtrackEventSourceUser
{
    public string? Username { get; set; }
    public string Id { get; set; } = null!;
}