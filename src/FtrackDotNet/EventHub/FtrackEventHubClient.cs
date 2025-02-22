using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FtrackDotNet.Api;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;
using Microsoft.Extensions.Options;
using Sprache;
using Action = System.Action;
using Task = System.Threading.Tasks.Task;

namespace FtrackDotNet.EventHub;

internal record Subscription(FtrackEventSource Source, string Expression, Action<FtrackEvent> Callback);

public class FtrackEventHubClient(
    IOptionsMonitor<FtrackOptions> options,
    ISocketIOFactory socketIoFactory,
    IFtrackClient ftrackClient)
    : IAsyncDisposable, IFtrackEventHubClient
{
    private readonly IDictionary<string, Subscription> _subscriptionsByExpression = new Dictionary<string, Subscription>();

    private ISocketIO? _socketIo;

    public event Action? OnConnect;

    public event Action? OnDisconnect;

    public event Action<Exception>? OnError;

    public Task PublishAsync(string topic, object data, string? target = null)
    {
        if (_socketIo == null)
        {
            throw new InvalidOperationException("Event hub not connected.");
        }

        var jsonSerializerOptions = FtrackContext.GetJsonSerializerOptions(JsonIgnoreCondition.WhenWritingNull);

        var sourceId = Guid.NewGuid().ToString();
        var payloadJson = JsonSerializer.Serialize(new FtrackEventEnvelope()
        {
            Name = "ftrack.event",
            Args = [new FtrackEvent()
            {
                Topic = topic,
                Target = target ?? string.Empty,
                Data = JsonSerializer.SerializeToElement(data),
                Source = GetEventSource(sourceId)
            }]
        }, jsonSerializerOptions);

        return _socketIo.EmitEventAsync(payloadJson);
    }

    public Task SubscribeAsync(string expression, Action<FtrackEvent> callback, string? subscriberId = null)
    {
        if (_socketIo == null)
        {
            throw new InvalidOperationException("Event hub not connected.");
        }
        
        if(_subscriptionsByExpression.ContainsKey(expression))
        {
            throw new InvalidOperationException("Already subscribed to expression: " + expression);
        }

        var parsedExpression = FtrackEventHubExpressionGrammar.Expression.TryParse(expression);
        if (!parsedExpression.WasSuccessful)
        {
            throw new InvalidOperationException("Invalid expression: " + expression);
        }

        subscriberId ??= Guid.NewGuid().ToString();
        _subscriptionsByExpression.Add(expression, new Subscription(
            GetEventSource(subscriberId), 
            expression, 
            callback));
        
        Debug.WriteLine("Subscribing to expression: " + expression + " with subscriber ID " + subscriberId);

        return PublishAsync(
            "ftrack.meta.subscribe",
            new
            {
                subscriber = new
                {
                    id = subscriberId
                },
                subscription = expression
            });
    }
    
    private FtrackEventSource GetEventSource(string sourceId)
    {
        return new FtrackEventSource()
        {
            Id = sourceId,
            ApplicationId = options.CurrentValue.EventHubApplicationId ?? "FtrackDotNet",
            User = new FtrackEventSourceUser()
            {
                Username = options.CurrentValue.ApiUser
            },
        };
    }

    public Task UnsubscribeAsync(string expression)
    {
        if (_socketIo == null)
        {
            throw new InvalidOperationException("Event hub not connected.");
        }
        
        if(!_subscriptionsByExpression.ContainsKey(expression))
        {
            return Task.CompletedTask;
        }

        var subscription = _subscriptionsByExpression[expression];
        Debug.WriteLine("Unsubscribing from expression: " + expression + " with subscriber ID " + subscription);
        
        return PublishAsync(
            "ftrack.meta.unsubscribe",
            new
            {
                subscriber = new
                {
                    id = subscription.Source.Id
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
        var result = payload.GetProperty("args");
        var subscriptions = _subscriptionsByExpression.Values;
        foreach (var eventElement in result.EnumerateArray())
        {
            var parsedEvent = eventElement.Deserialize<FtrackEvent>(FtrackContext.GetJsonSerializerOptions())!;
            var parsedEventTargetExpression = !string.IsNullOrWhiteSpace(parsedEvent.Target) ?
                FtrackEventHubExpressionGrammar.Expression.Parse(parsedEvent.Target) :
                null;
            foreach(var subscription in subscriptions)
            {
                var subscriptionElement = JsonSerializer.SerializeToElement(subscription.Source, FtrackContext.GetJsonSerializerOptions());
                if (parsedEventTargetExpression != null && !parsedEventTargetExpression.Evaluate(subscriptionElement))
                {
                    continue;
                }
                
                var parsedSubscriptionExpression = FtrackEventHubExpressionGrammar.Expression.Parse(subscription.Expression);
                if (!parsedSubscriptionExpression.Evaluate(eventElement))
                {
                    continue;
                }

                subscription.Callback(parsedEvent);
            }
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