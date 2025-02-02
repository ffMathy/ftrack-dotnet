using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FtrackDotNet.Tests.Linq;

[TestClass]
public class FtrackContextTest
{
    [TestMethod]
    public async Task ToArrayAsync_SimpleQuery_ReturnsResults()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder();
        hostBuilder.ConfigureAppConfiguration(x => x
            .AddUserSecrets<FtrackContextTest>());
        hostBuilder.ConfigureServices(services =>
            services.AddFtrack());
        
        using var host = hostBuilder.Build();
        await using var scope = host.Services.CreateAsyncScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (configuration.GetChildren().All(x => x.Key != "Ftrack"))
        {
            Console.WriteLine("Skipping test due to no configuration found.");
            return;
        }
        
        var ftrackContext = scope.ServiceProvider.GetRequiredService<FtrackContext>();

        // Act
        var entities = await ftrackContext.TypedContexts
            .Select(t => new { t.Name })
            .ToArrayAsync();

        // Assert
        Assert.AreNotEqual(0, entities.Length);
    }
}