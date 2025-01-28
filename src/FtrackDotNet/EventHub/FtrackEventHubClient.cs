using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.Json;
using System.Web;
using FtrackDotNet.Clients;
using FtrackDotNet.Models;
using Microsoft.Extensions.Options;
using Action = System.Action;

namespace FtrackDotNet.EventHub;

/// <summary>
/// Mimics the JavaScript event_hub.ts class using our SocketIO class.
/// </summary>
public class FtrackEventHubClient : IAsyncDisposable, IFtrackEventHubClient
{
    private readonly IOptionsMonitor<FtrackOptions> _options;
    private readonly ISocketIOFactory _socketIoFactory;
    private readonly IFtrackClient _ftrackClient;
    
    private readonly IDictionary<string, string> _subscriptionIdsByTopic = new Dictionary<string, string>();

    private ISocketIO? _socketIo;

    /// <summary>
    /// Unique ID for this EventHub instance (guid).
    /// </summary>
    public string Id { get; }

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
    /// Fired when a specific topic event is received.
    /// The first parameter is the topic name, the second is the event.
    /// </summary>
    public event Action<string, FtrackEvent>? OnTopicEvent;

    public FtrackEventHubClient(
        IOptionsMonitor<FtrackOptions> options,
        ISocketIOFactory socketIoFactory,
        IFtrackClient ftrackClient)
    {
        _options = options;
        _socketIoFactory = socketIoFactory;
        _ftrackClient = ftrackClient;

        // Generate a random UUID for this event hub instance
        Id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Build the query string for the event hub URL (mimics new URLSearchParams in JS).
    /// </summary>
    private static string BuildQueryString(Dictionary<string, string?> parameters)
    {
        var list = new List<string>();
        foreach (var keyValuePair in parameters)
        {
            if (keyValuePair.Value == null)
                continue;

            var key = Uri.EscapeDataString(keyValuePair.Key);
            var value = Uri.EscapeDataString(keyValuePair.Value);
            list.Add($"{key}={value}");
        }

        return string.Join("&", list);
    }

    /// <summary>
    /// Parse the incoming data as an Ftrack event, raise OnEventReceived, OnTopicEvent, etc.
    /// </summary>
    private void HandleEventData(object data)
    {
        // In JavaScript, data is presumably an object that has topic, data, etc.
        // We'll try to parse it as JSON or handle it as a dictionary.
        // If your SocketIO already passes in a .NET object, adapt as needed.
        try
        {
            // data might already be a dictionary or a string. 
            // If it's a raw string, we can parse it as JSON:
            if (data is string dataStr)
            {
                var ftrackEvent = JsonSerializer.Deserialize<FtrackEvent>(dataStr);
                if (ftrackEvent == null)
                    return;

                RaiseEvent(ftrackEvent);
            }
            else if (data is FtrackEvent typedEvent)
            {
                // If your code is already passing a typed event
                RaiseEvent(typedEvent);
            }
            else
            {
                // If it's some other shape (dictionary?), handle accordingly
                // e.g. convert to JSON then back to FtrackEvent
                var json = JsonSerializer.Serialize(data);
                var ftrackEvent = JsonSerializer.Deserialize<FtrackEvent>(json);
                if (ftrackEvent == null)
                    return;

                RaiseEvent(ftrackEvent);
            }
        }
        catch
        {
            // swallow or rethrow/log
        }
    }

    private void RaiseEvent(FtrackEvent evt)
    {
        OnEventReceived?.Invoke(evt);

        if (!string.IsNullOrEmpty(evt.Topic))
        {
            OnTopicEvent?.Invoke(evt.Topic, evt);
        }
    }

    /// <summary>
    /// Corresponds to `publish(event)` in the JS code.
    /// Calls socket.emit('publish', event).
    /// </summary>
    public Task PublishAsync(FtrackEvent @event)
    {
        if (_socketIo == null)
        {
            throw new InvalidOperationException("Event hub not connected.");
        }

        @event.Source ??= new FtrackEventSource();
        @event.Source.Id ??= Id;
        @event.Source.ApplicationId ??= "FtrackDotNet";
        @event.Source.User ??= new FtrackEventSourceUser();
        @event.Source.User.Username ??= _options.CurrentValue.ApiUser;

        return _socketIo.EmitMessageAsync("ftrack.event", @event);
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
        
        return _socketIo.EmitMessageAsync("ftrack.meta.subscribe", new
        {
            subscriber = new
            {
                id = subscriberId
            },
            subscription = $"topic={expression}",
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
        
        return _socketIo.EmitMessageAsync("ftrack.meta.unsubscribe", new
        {
            subscriber = new
            {
                id = subscriberId
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
            ["api_user"] = _options.CurrentValue.ApiUser,
            ["api_key"] = _options.CurrentValue.ApiKey
        };
        var builder = new UriBuilder(_options.CurrentValue.ServerUrl)
        {
            Path = $"/socket.io/1/websocket/{sessionId}",
            Query = query.ToString()
        };
        builder.Scheme = builder.Scheme == "https" ? "wss" : "ws";

        _socketIo = _socketIoFactory.Create(builder.Uri);

        // Listen to low-level socket events
        _socketIo.OnConnect += () => OnConnect?.Invoke();
        _socketIo.OnDisconnect += () => OnDisconnect?.Invoke();
        _socketIo.OnError += ex => OnError?.Invoke(ex);

        // This is the crucial part: the JS code says:
        //    this.socket.on('event', (event) => this.handleEvent(event));
        // We'll do the same, listening for "event".
        _socketIo.OnEvent += (eventName, data) =>
        {
            // In ftrack's code, they only handle `eventName == "event"`.
            if (eventName == "event" && data != null)
            {
                HandleEventData(data);
            }
        };

        await _socketIo.ConnectAsync();
    }

    private async Task<string> GetSessionIdAsync()
    {
        var responseString = await _ftrackClient.MakeRawRequestAsync(
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