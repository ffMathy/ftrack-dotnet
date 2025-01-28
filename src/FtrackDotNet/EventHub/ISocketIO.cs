namespace FtrackDotNet.EventHub;

public interface ISocketIO : IAsyncDisposable
{
    event Action? OnConnect;
    event Action? OnDisconnect;
    event Action<Exception>? OnError;
    event Action<string, object?>? OnEvent;

    /// <summary>
    /// Initiates the connection, starts heartbeat, then enters receive loop.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Send an event with the format "42[eventName, data]".
    /// </summary>
    Task EmitMessageAsync(string eventName, object? data);

    /// <summary>
    /// Closes the socket cleanly.
    /// </summary>
    Task CloseAsync();
}