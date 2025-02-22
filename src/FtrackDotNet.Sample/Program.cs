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

hub.OnError += ex => Console.WriteLine($"[Error] {ex.Message}");

await hub.ConnectAsync();

await hub.SubscribeAsync(
    "topic=my.custom.topic", 
    evt =>
    {
        Console.WriteLine($"[my.custom.topic]: {evt.Data}");
    },
    "my-custom-subscriber-id");

await hub.SubscribeAsync(
    "topic=ftrack.update",
    evt =>
    {
        Console.WriteLine($"[ftrack.update]: {evt.Data}");
    });

await hub.PublishAsync(
    "my.custom.topic",
    "Hello from .NET!",
    "id=my-custom-subscriber-id"
);

Console.WriteLine("Press ENTER to update a project's name...");
Console.ReadLine();

var ftrackContext = scope.ServiceProvider.GetRequiredService<CustomFtrackContext>();

var firstProject = await ftrackContext.Projects.FirstOrDefaultAsync();
firstProject.Name = Guid.NewGuid().ToString();
await ftrackContext.SaveChangesAsync();

Console.WriteLine("Press ENTER to quit...");
Console.ReadLine();

await hub.UnsubscribeAsync("my.custom.topic");
await hub.DisconnectAsync();