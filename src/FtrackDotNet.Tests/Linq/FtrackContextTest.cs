using FtrackDotNet.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace FtrackDotNet.Tests;

[TestClass]
public class FtrackContextTest
{
    [TestMethod]
    public async Task Translate_SimplePropertyInWhere_ReturnsCorrectQuery()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder();
        hostBuilder.ConfigureAppConfiguration(x => x
            .AddUserSecrets<FtrackContextTest>());
        hostBuilder.ConfigureServices(services =>
            services.AddFtrack());
        
        using var host = hostBuilder.Build();
        await using var scope = host.Services.CreateAsyncScope();

        var ftrackContext = scope.ServiceProvider.GetRequiredService<FtrackContext>();

        // Act
        var entities = await ftrackContext.TypedContexts
            .Where(t => t.Name == "foobar")
            .Select(t => new { t.Name })
            .ToArrayAsync();

        // Assert
        Assert.AreEqual(0, entities.Length);
    }
}