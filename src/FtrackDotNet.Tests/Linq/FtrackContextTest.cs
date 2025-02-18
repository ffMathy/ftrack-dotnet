using FtrackDotNet.Extensions;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Task = System.Threading.Tasks.Task;

namespace FtrackDotNet.Tests.Linq;

[TestClass]
public class FtrackContextTest
{
    [TestMethod]
    public async Task ToArrayAsync_SimpleQuery_ReturnsResults()
    {
        // Arrange
        await using var scope = StartHost();

        var ftrackContext = scope.ServiceProvider.GetRequiredService<CustomFtrackContext>();

        // Act
        var entities = await ftrackContext.TypedContexts
            .Select(t => new { t.Name })
            .ToArrayAsync();

        // Assert
        Assert.AreNotEqual(0, entities.Length);
    }

    [TestMethod]
    public async Task UpdateAsync_NameFetchedAndChanged_UpdatesName()
    {
        // Arrange
        await using var scope = StartHost();

        var ftrackContext = scope.ServiceProvider.GetRequiredService<CustomFtrackContext>();

        var project = ftrackContext.Projects.Add(new Project()
        {
            Name = Guid.NewGuid().ToString()
        });
        await ftrackContext.SaveChangesAsync();

        // Act
        project.Name = "new name";
        await ftrackContext.SaveChangesAsync();

        // Assert
        var refreshedEntity = await ftrackContext.Projects
            .Where(t => t.Id == project.Id)
            .Select(t => new { t.Name })
            .SingleAsync();
        Assert.AreEqual("new name", refreshedEntity.Name);
    }

    private static AsyncServiceScope StartHost()
    {
        var hostBuilder = Host.CreateDefaultBuilder();
        hostBuilder.ConfigureAppConfiguration(x => x
            .AddUserSecrets<FtrackContextTest>());
        hostBuilder.ConfigureServices(services =>
            services.AddFtrack<CustomFtrackContext>());

        var host = hostBuilder.Build();
        var scope = host.Services.CreateAsyncScope();
        
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (configuration.GetChildren().All(x => x.Key != "Ftrack"))
        {
            throw new InvalidOperationException("Could not find Ftrack configuration to use for testing.");
        }

        return scope;
    }
}