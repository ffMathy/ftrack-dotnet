using System.Text.Json;

namespace FtrackDotNet.EventHub;

public interface ISocketIO : IAsyncDisposable
{
    event Action? OnConnect;
    event Action? OnDisconnect;
    event Action<Exception>? OnError;
    event Action<JsonElement>? OnEvent;

    /// <summary>
    /// Initiates the connection, starts heartbeat, then enters receive loop.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Send an event with the format "42[eventName, data]".
    /// </summary>
    Task EmitEventAsync(string fullMessage);

    /// <summary>
    /// Closes the socket cleanly.
    /// </summary>
    Task CloseAsync();
}