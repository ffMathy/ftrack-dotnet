using FtrackDotNet.EventHub;
using FtrackDotNet.Extensions;
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

hub.OnEventReceived += evt =>
{
    Console.WriteLine($"[EventReceived] Topic={evt.Topic}, Data={evt.Data}");
};

hub.OnError += ex => Console.WriteLine($"[Error] {ex.Message}");

await hub.ConnectAsync();

// Subscribe to two events.
await hub.SubscribeAsync("topic=my.custom.topic", "my-custom-subscriber-id");
await hub.SubscribeAsync("topic=ftrack.update");

// Publish an event
await hub.PublishAsync(
    "my.custom.topic",
    "Hello from .NET!",
    "id=my-custom-subscriber-id"
);

var ftrackContext = scope.ServiceProvider.GetRequiredService<CustomFtrackContext>();

// Update the name of a project
var firstProject = await ftrackContext.Projects.FirstOrDefaultAsync();
firstProject.Name = Guid.NewGuid().ToString();
await ftrackContext.SaveChangesAsync();

Console.WriteLine("Press ENTER to quit...");
Console.ReadLine();

// Unsubscribe & close
await hub.UnsubscribeAsync("my.custom.topic");
await hub.DisconnectAsync(); // Or just let DisposeAsync() handle it