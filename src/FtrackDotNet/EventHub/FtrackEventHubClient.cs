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

public class FtrackEventHubClient(
    IOptionsMonitor<FtrackOptions> options,
    ISocketIOFactory socketIoFactory,
    IFtrackClient ftrackClient)
    : IAsyncDisposable, IFtrackEventHubClient
{
    private readonly IDictionary<string, string> _subscriptionIdsByExpression = new Dictionary<string, string>();

    private ISocketIO? _socketIo;

    public string Id { get; } = Guid.NewGuid().ToString();

    public event Action? OnConnect;

    public event Action? OnDisconnect;

    public event Action<Exception>? OnError;

    public event Action<FtrackEvent>? OnEventReceived;

    public Task PublishAsync(string topic, object data, string? target = null)
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
                Topic = topic,
                Target = target ?? string.Empty,
                Data = JsonSerializer.SerializeToElement(data, jsonSerializerOptions),
                Source = new FtrackEventSource()
                {
                    Id = Id,
                    ApplicationId = options.CurrentValue.EventHubApplicationId ?? "FtrackDotNet",
                    User = new FtrackEventSourceUser()
                    {
                        Username = options.CurrentValue.ApiUser
                    },
                }
            }]
        }, jsonSerializerOptions);

        return _socketIo.EmitEventAsync(payloadJson);
    }

    public Task SubscribeAsync(string expression, string? subscriberId = null)
    {
        if (_socketIo == null)
        {
            throw new InvalidOperationException("Event hub not connected.");
        }
        
        if(_subscriptionIdsByExpression.ContainsKey(expression))
        {
            throw new InvalidOperationException("Already subscribed to expression: " + expression);
        }

        subscriberId ??= Guid.NewGuid().ToString();
        _subscriptionIdsByExpression.Add(expression, subscriberId);
        
        Debug.WriteLine("Subscribing to expression: " + expression + " with subscriber ID " + subscriberId);

        return PublishAsync(
            "ftrack.meta.subscribe",
            new
            {
                Subscriber = new
                {
                    Id = subscriberId
                },
                Subscription = expression
            });
    }

    public Task UnsubscribeAsync(string expression)
    {
        if (_socketIo == null)
        {
            throw new InvalidOperationException("Event hub not connected.");
        }
        
        if(!_subscriptionIdsByExpression.ContainsKey(expression))
        {
            return Task.CompletedTask;
        }

        var subscriberId = _subscriptionIdsByExpression[expression];
        Debug.WriteLine("Unsubscribing from expression: " + expression + " with subscriber ID " + subscriberId);
        
        return PublishAsync(
            "ftrack.meta.unsubscribe",
            new
            {
                Subscriber = new
                {
                    Id = subscriberId
                },
            });
    }
    
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

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}