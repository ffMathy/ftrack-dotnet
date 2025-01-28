using FtrackDotNet.EventHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = Host.CreateDefaultBuilder();
hostBuilder.ConfigureAppConfiguration(x => x
    .AddUserSecrets<Program>());
hostBuilder.ConfigureServices(services =>
    services.AddFtrack());
        
using var host = hostBuilder.Build();
await using var scope = host.Services.CreateAsyncScope();

var hub = scope.ServiceProvider.GetRequiredService<IFtrackEventHubClient>();

hub.OnConnect += () => Console.WriteLine("Hub connected!");
hub.OnDisconnect += () => Console.WriteLine("Hub disconnected!");

// Fired on ANY incoming event
hub.OnEventReceived += evt =>
{
    Console.WriteLine($"[EventReceived] Topic={evt.Topic}, Data={evt.Data}");
};

// Fired specifically for the event's topic
hub.OnTopicEvent += (topic, evt) =>
{
    Console.WriteLine($"[TopicEvent] {topic}, Data={evt.Data}");
};

hub.OnError += ex => Console.WriteLine($"[Error] {ex.Message}");

await hub.ConnectAsync();

// Subscribe to a test topic
await hub.SubscribeAsync("my.custom.topic");

// Publish an event
await hub.PublishAsync(new FtrackEvent
{
    Topic = "my.custom.topic",
    Data = "Hello from .NET!"
});

Console.WriteLine("Press ENTER to quit...");
Console.ReadLine();

// Unsubscribe & close
await hub.UnsubscribeAsync("my.custom.topic");
await hub.DisconnectAsync(); // Or just let DisposeAsync() handle it