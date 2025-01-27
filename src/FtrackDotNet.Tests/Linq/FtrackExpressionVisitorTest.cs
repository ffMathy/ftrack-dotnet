using FtrackDotNet.Linq;
using Moq;

namespace FtrackDotNet.Tests;

internal class FtrackTask
{
    public double Bid { get; set; }
    public string Name { get; set; }

    public FtrackTask[] Children { get; set; }

    public FtrackTask Parent { get; set; }
}

[TestClass]
public class FtrackExpressionVisitorTest
{
    [TestMethod]
    public async Task Translate_HighwayTest_ReturnsCorrectQuery()
    {
        // Arrange
        var mockClient = new Mock<IFtrackClient>();
        var queryable = new FtrackQueryable<FtrackTask>(new FtrackQueryProvider(mockClient.Object));

        // Act
        await queryable
            .Where(t => 
                t.Bid > 10 && 
                (t.Name.StartsWith("foo") && t.Name.EndsWith("bar")) &&
                t.Name.Contains("foobar") &&
                (t.Parent.Parent.Name == "baz" || t.Parent.Children.Any(x => x.Name == "fuz")))
            .Select(t => new { t.Name, t.Bid })
            .OrderByDescending(x => x.Name)
            .Skip(5)
            .Take(10)
            .ToArrayAsync();

        // Assert
        mockClient.Verify(
            client => client.QueryAsync<object>("select name, bid from FtrackTask where bid > 10 order by name descending offset 5 limit 10"), 
            Times.Once);
    }
}