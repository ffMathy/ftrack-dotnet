using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FtrackDotNet.Api;
using FtrackDotNet.UnitOfWork;
using Microsoft.Extensions.Options;
using Action = System.Action;
using Task = System.Threading.Tasks.Task;

namespace FtrackDotNet.EventHub;

/// <summary>
/// Mimics the JavaScript event_hub.ts class using our SocketIO class.
/// </summary>
public class FtrackEventHubClient(
    IOptionsMonitor<FtrackOptions> options,
    ISocketIOFactory socketIoFactory,
    IFtrackClient ftrackClient)
    : IAsyncDisposable, IFtrackEventHubClient
{
    private readonly IDictionary<string, string> _subscriptionIdsByTopic = new Dictionary<string, string>();

    private ISocketIO? _socketIo;

    /// <summary>
    /// Unique ID for this EventHub instance (guid).
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Fired when the underlying socket connects.
    /// </summary>
    public event Action? OnConnect;

    /// <summary>
    /// Fired when the underlying socket disconnects.
    /// </summary>
    public event Action? OnDisconnect;

    /// <summary>
    /// Fired when the underlying socket encounters an error.
    /// </summary>
    public event Action<Exception>? OnError;

    /// <summary>
    /// Fired when *any* event is received from the server.
    /// </summary>
    public event Action<FtrackEvent>? OnEventReceived;

    /// <summary>
    /// Corresponds to `publish(event)` in the JS code.
    /// Calls socket.emit('publish', event).
    /// </summary>
    public Task PublishAsync(string topic, object data)
    {
        if (_socketIo == null)
        {
            throw new InvalidOperationException("Event hub not connected.");
        }

        var jsonSerializerOptions = FtrackContext.GetJsonSerializerOptions(JsonIgnoreCondition.WhenWritingNull);
        var payloadJson = JsonSerializer.Serialize(new FtrackEventEnvelope()
        {
            Name = "ftrack.event",
            Args = [new FtrackEvent()
            {
                Data = JsonSerializer.SerializeToElement(data, jsonSerializerOptions),
                Source = new FtrackEventSource()
                {
                    Id = Id,
                    ApplicationId = "FtrackDotNet",
                    User = new FtrackEventSourceUser()
                    {
                        Username = options.CurrentValue.ApiUser
                    }
                }
            }]
        }, jsonSerializerOptions);

        return _socketIo.EmitEventAsync(payloadJson);
    }

    /// <summary>
    /// Corresponds to `subscribe(topic)` in the JS code.
    /// Calls socket.emit('subscribe', topic).
    /// </summary>
    public Task SubscribeAsync(string expression)
    {
        if (_socketIo == null)
        {
            throw new InvalidOperationException("Event hub not connected.");
        }
        
        if(_subscriptionIdsByTopic.ContainsKey(expression))
        {
            throw new InvalidOperationException("Already subscribed to topic: " + expression);
        }

        var subscriberId = Guid.NewGuid().ToString();
        _subscriptionIdsByTopic.Add(expression, subscriberId);
        
        Debug.WriteLine("Subscribing to topic: " + expression + " with subscriber ID " + subscriberId);

        return PublishAsync(
            "ftrack.meta.subscribe",
            new
            {
                Subscriber = new
                {
                    Id = subscriberId
                },
                Subscription = $"topic={expression}",
            });
    }

    /// <summary>
    /// Corresponds to `unsubscribe(topic)` in the JS code.
    /// Calls socket.emit('unsubscribe', topic).
    /// </summary>
    public Task UnsubscribeAsync(string topic)
    {
        if (_socketIo == null)
        {
            throw new InvalidOperationException("Event hub not connected.");
        }
        
        if(!_subscriptionIdsByTopic.ContainsKey(topic))
        {
            return Task.CompletedTask;
        }

        var subscriberId = _subscriptionIdsByTopic[topic];
        Debug.WriteLine("Unsubscribing from topic: " + topic + " with subscriber ID " + subscriberId);
        
        return PublishAsync(
            "ftrack.meta.subscribe",
            new
            {
                Subscriber = new
                {
                    Id = subscriberId
                },
            });
    }

    /// <summary>
    /// Corresponds to `connect()` in JS. 
    /// Simply calls the underlying socket's ConnectAsync.
    /// </summary>
    public async Task ConnectAsync()
    {
        var sessionId = await GetSessionIdAsync();

        var query = new NameValueCollection
        {
            ["EIO"] = "4",
            ["transport"] = "websocket",
            ["api_user"] = options.CurrentValue.ApiUser,
            ["api_key"] = options.CurrentValue.ApiKey
        };
        var builder = new UriBuilder(options.CurrentValue.ServerUrl)
        {
            Path = $"/socket.io/1/websocket/{sessionId}",
            Query = query.ToString()
        };
        builder.Scheme = builder.Scheme == "https" ? "wss" : "ws";

        await DisconnectAsync();
        
        _socketIo = socketIoFactory.Create(builder.Uri);

        _socketIo.OnConnect += FireOnConnect;
        _socketIo.OnDisconnect += FireOnDisconnect;
        _socketIo.OnError += FireOnError;
        _socketIo.OnEvent += FireOnEvent;

        await _socketIo.ConnectAsync();
    }

    private void FireOnEvent(JsonElement payload)
    {
        var result = JsonSerializer.Deserialize<FtrackEventEnvelope>(payload.ToString(), FtrackContext.GetJsonSerializerOptions());
        foreach (var @event in result.Args)
        {
            OnEventReceived?.Invoke(@event);
        }
    }

    private void FireOnError(Exception ex)
    {
        OnError?.Invoke(ex);
    }

    private void FireOnDisconnect()
    {
        OnDisconnect?.Invoke();
    }

    private void FireOnConnect()
    {
        OnConnect?.Invoke();
    }

    private async Task<string> GetSessionIdAsync()
    {
        var responseString = await ftrackClient.MakeRawRequestAsync(
            HttpMethod.Get,
            "socket.io/1/");
        var sessionId = responseString.Split(":").First();
        return sessionId;
    }

    /// <summary>
    /// Corresponds to `disconnect()` in JS.
    /// Closes the underlying socket.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_socketIo == null)
        {
            return;
        }

        await _socketIo.CloseAsync();
        await _socketIo.DisposeAsync();

        _socketIo.OnConnect -= FireOnConnect;
        _socketIo.OnDisconnect -= FireOnDisconnect;
        _socketIo.OnError -= FireOnError;
        _socketIo.OnEvent -= FireOnEvent;
        
        _socketIo = null;
    }

    /// <summary>
    /// Dispose resources in an async-friendly manner.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}