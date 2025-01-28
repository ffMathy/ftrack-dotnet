namespace FtrackDotNet.EventHub;

public interface IFtrackEventHubClient : IAsyncDisposable
{
    /// <summary>
    /// Fired when the underlying socket encounters an error.
    /// </summary>
    event Action<Exception>? OnError;

    /// <summary>
    /// Fired when the underlying socket connects.
    /// </summary>
    event Action? OnConnect;

    /// <summary>
    /// Fired when the underlying socket disconnects.
    /// </summary>
    event Action? OnDisconnect;

    /// <summary>
    /// Fired when *any* event is received from the server.
    /// </summary>
    event Action<FtrackEvent>? OnEventReceived;

    /// <summary>
    /// Fired when a specific topic event is received.
    /// The first parameter is the topic name, the second is the event.
    /// </summary>
    event Action<string, FtrackEvent>? OnTopicEvent;

    /// <summary>
    /// Corresponds to `publish(event)` in the JS code.
    /// Calls socket.emit('publish', event).
    /// </summary>
    Task PublishAsync(FtrackEvent @event);

    /// <summary>
    /// Corresponds to `subscribe(topic)` in the JS code.
    /// Calls socket.emit('subscribe', topic).
    /// </summary>
    Task SubscribeAsync(string expression);

    /// <summary>
    /// Corresponds to `unsubscribe(topic)` in the JS code.
    /// Calls socket.emit('unsubscribe', topic).
    /// </summary>
    Task UnsubscribeAsync(string topic);

    /// <summary>
    /// Corresponds to `connect()` in JS. 
    /// Simply calls the underlying socket's ConnectAsync.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Corresponds to `disconnect()` in JS.
    /// Closes the underlying socket.
    /// </summary>
    Task DisconnectAsync();
}