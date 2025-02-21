using System.Text.Json;
using FtrackDotNet.Api;
using FtrackDotNet.Extensions;
using FtrackDotNet.Linq;
using FtrackDotNet.UnitOfWork;
using Moq;

namespace FtrackDotNet.Tests.Linq;

// ReSharper disable once ClassNeverInstantiated.Global
internal class FtrackTask
{
    public double Bid { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset Time { get; set; }

    public FtrackTask[] Children { get; set; } = null!;

    public FtrackTask Parent { get; set; } = null!;
}

[TestClass]
public class FtrackExpressionVisitorTest
{
    private readonly Mock<IFtrackClient> _mockFtrackClient;
    private readonly FtrackQueryable<FtrackTask> _queryable;

    private string SanitizeMultilineQuery(params string[] lines)
    {
        return lines
            .Select(x => x.Trim())
            .Aggregate((x, y) => $"{x} {y}");
    }

    public FtrackExpressionVisitorTest()
    {
        var mockClient = new Mock<IFtrackClient>();
        _mockFtrackClient = mockClient;
        _queryable = new FtrackQueryable<FtrackTask>(new FtrackQueryProvider(
            mockClient.Object,
            new Mock<IChangeTracker>().Object));
    }
    
    [TestMethod]
    public async Task Translate_SimplePropertyInWhere_ReturnsCorrectQuery()
    {
        // Arrange
        _mockFtrackClient
            .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => [JsonSerializer.SerializeToElement(Array.Empty<object>())]);
        
        // Act
        await _queryable
            .Where(t => t.Bid > 10)
            .Select(t => new { t.Name })
            .ToArrayAsync();

        // Assert
        var query = SanitizeMultilineQuery(
            "select name from FtrackTask where",
            "(bid > 10)");
        _mockFtrackClient.Verify(
            client => client.QueryAsync(query, CancellationToken.None), 
            Times.Once);
    }
    
    [TestMethod]
    public async Task Translate_TimePropertyInWhere_ReturnsCorrectQuery()
    {
        // Arrange
        _mockFtrackClient
            .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => [JsonSerializer.SerializeToElement(Array.Empty<object>())]);
        
        var dateTimeOffset = new DateTimeOffset(2024, 01, 01, 0, 0, 0, TimeSpan.Zero);
        
        // Act
        await _queryable
            .Where(t => t.Time < dateTimeOffset)
            .Select(t => new { t.Name })
            .ToArrayAsync();

        // Assert
        var query = SanitizeMultilineQuery(
            "select name from FtrackTask where (time < \"2024-01-01T00:00:00\")");
        _mockFtrackClient.Verify(
            client => client.QueryAsync(query, CancellationToken.None), 
            Times.Once);
    }
    
    [TestMethod]
    public async Task Translate_MultiplePropertiesInWhere_ReturnsCorrectQuery()
    {
        // Arrange
        _mockFtrackClient
            .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => [JsonSerializer.SerializeToElement(Array.Empty<object>())]);
        
        // Act
        await _queryable
            .Where(t => t.Bid > 10 && t.Name == "foobar")
            .Select(t => new { t.Name })
            .ToArrayAsync();

        // Assert
        var query = SanitizeMultilineQuery(
            "select name from FtrackTask where",
            "((bid > 10) and",
            "(name = \"foobar\"))");
        _mockFtrackClient.Verify(
            client => client.QueryAsync(query, CancellationToken.None), 
            Times.Once);
    }
    
    [TestMethod]
    public async Task Translate_HighwayTest_ReturnsCorrectQuery()
    {
        // Arrange
        _mockFtrackClient
            .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => [JsonSerializer.SerializeToElement(Array.Empty<object>())]);
        
        // Act
        await _queryable
            .Where(t => 
                t.Bid > 10 && 
                (t.Name.StartsWith("foo") && t.Name.EndsWith("bar")) &&
                t.Name.Contains("foobar") &&
                (t.Parent.Parent.Name == "baz" || t.Parent.Children.Any(x => x.Name == "fuz" && x.Parent.Name == "blah")))
            .Select(t => new { t.Name, t.Bid })
            .OrderByDescending(x => x.Name)
            .Skip(5)
            .Take(10)
            .ToArrayAsync();

        // Assert
        var query = SanitizeMultilineQuery(
            "select name, bid from FtrackTask where ((((bid > 10) and (name like \"%foo\" and name like \"bar%\")) and name like \"%foobar%\") and ((parent.parent.name = \"baz\") or parent.children any (((name = \"fuz\") and (parent.name = \"blah\"))))) order by name descending offset 5 limit 10");
        _mockFtrackClient.Verify(
            client => client.QueryAsync(query, CancellationToken.None), 
            Times.Once);
    }
}