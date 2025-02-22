using FtrackDotNet.EventHub;
using FtrackDotNet.Sample;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = Host.CreateDefaultBuilder();
hostBuilder.ConfigureAppConfiguration(x => x
    .AddUserSecrets<Program>());
hostBuilder.ConfigureServices(services =>
    services.AddFtrack<CustomFtrackContext>());

using var host = hostBuilder.Build();
await using var scope = host.Services.CreateAsyncScope();

var hub = scope.ServiceProvider.GetRequiredService<IFtrackEventHubClient>();

hub.OnConnect += () => Console.WriteLine("Hub connected!");
hub.OnDisconnect += () => Console.WriteLine("Hub disconnected!");

// Fired on ANY incoming event
hub.OnEventReceived += evt => { Console.WriteLine($"[EventReceived] Topic={evt.Topic}, Data={evt.Data}"); };

hub.OnError += ex => Console.WriteLine($"[Error] {ex.Message}");

await hub.ConnectAsync();

await hub.SubscribeAsync("my.custom.topic");
await hub.SubscribeAsync("ftrack.update");

// Publish an event
await hub.PublishAsync(
    "my.custom.topic",
    "Hello from .NET!"
);

var ftrackContext = scope.ServiceProvider.GetRequiredService<CustomFtrackContext>();

Console.WriteLine("Press ENTER to quit...");
Console.ReadLine();

// Unsubscribe & close
await hub.UnsubscribeAsync("my.custom.topic");
await hub.DisconnectAsync(); // Or just let DisposeAsync() handle it