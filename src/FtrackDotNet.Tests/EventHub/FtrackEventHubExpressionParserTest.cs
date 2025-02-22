using System.Text.Json;
using FtrackDotNet.EventHub;
using Sprache;

namespace FtrackDotNet.Tests.EventHub;

[TestClass]
public class FtrackEventHubExpressionParserTest
{
    [TestMethod]
    public async Task Parse_SimpleProperty_IsValid()
    {
        // Arrange
        var expression = "foo=bar";

        var trueElement = JsonSerializer.SerializeToElement(new
        {
            foo = "bar"
        });

        var falseElement = JsonSerializer.SerializeToElement(new
        {
            foo = "not-bar"
        });
        
        // Act
        var result = FtrackEventHubExpressionGrammar.Expression.TryParse(expression);
        
        // Assert
        Assert.IsTrue(result.WasSuccessful);
        
        Assert.IsTrue(result.Value.Evaluate(trueElement));
        Assert.IsFalse(result.Value.Evaluate(falseElement));
    }
    
    [TestMethod]
    public async Task Parse_WildcardedProperty_IsValid()
    {
        // Arrange
        var expression = "foo=fuz*";

        var trueElement = JsonSerializer.SerializeToElement(new
        {
            foo = "fuzzy"
        });

        var falseElement = JsonSerializer.SerializeToElement(new
        {
            foo = "fubby"
        });
        
        // Act
        var result = FtrackEventHubExpressionGrammar.Expression.TryParse(expression);
        
        // Assert
        Assert.IsTrue(result.WasSuccessful);
        
        Assert.IsTrue(result.Value.Evaluate(trueElement));
        Assert.IsFalse(result.Value.Evaluate(falseElement));
    }
    
    [TestMethod]
    public async Task Parse_MultiplePropertiesSeparatedByAndOperator_IsValid()
    {
        // Arrange
        var expression = "foo=bar and buz=baz";

        var trueElement = JsonSerializer.SerializeToElement(new
        {
            foo = "bar",
            buz = "baz"
        });

        var falseElement = JsonSerializer.SerializeToElement(new
        {
            foo = "not-bar",
            buz = "baz"
        });
        
        // Act
        var result = FtrackEventHubExpressionGrammar.Expression.TryParse(expression);
        
        // Assert
        Assert.IsTrue(result.WasSuccessful);
        
        Assert.IsTrue(result.Value.Evaluate(trueElement));
        Assert.IsFalse(result.Value.Evaluate(falseElement));
    }
    
    [TestMethod]
    public async Task Parse_MultiplePropertiesSeparatedByOrOperator_IsValid()
    {
        // Arrange
        var expression = "foo=bar or buz=baz";

        var trueElement = JsonSerializer.SerializeToElement(new
        {
            foo = "bar",
            buz = "baz"
        });

        var falseElement = JsonSerializer.SerializeToElement(new
        {
            foo = "not-bar",
            buz = "not-baz"
        });
        
        // Act
        var result = FtrackEventHubExpressionGrammar.Expression.TryParse(expression);
        
        // Assert
        Assert.IsTrue(result.WasSuccessful);
        
        Assert.IsTrue(result.Value.Evaluate(trueElement));
        Assert.IsFalse(result.Value.Evaluate(falseElement));
    }
    
    [TestMethod]
    public async Task Parse_HighwayTestExpression_IsValid()
    {
        // Arrange
        var expression = "foo=bar or buz=baz and (blah = lala or (abc = def))";
        
        // Act
        var result = FtrackEventHubExpressionGrammar.Expression.TryParse(expression);
        
        // Assert
        Assert.IsTrue(result.WasSuccessful);
    }
}