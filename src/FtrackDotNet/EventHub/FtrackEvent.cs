namespace FtrackDotNet.EventHub;

/// <summary>
/// Represents an event structure coming from Ftrack's event hub.
/// </summary>
public class FtrackEvent
{
    public string? Topic { get; set; }
    public string? Data { get; set; }
}