using FtrackDotNet.Linq;

namespace FtrackDotNet.Tests;

internal class Task
{
    public double Bid { get; set; }
    public string Name { get; set; }

    public Task[] Children { get; set; }

    public Task Parent { get; set; }
}

[TestClass]
public class FtrackExpressionVisitorTest
{
    [TestMethod]
    public void Translate_TableWithoutConditions_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x);

        // assert
        Assert.AreEqual("Task", query);
    }

    [TestMethod]
    public void Translate_TableWithEqualityConditionOnNumber_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Bid == 27);

        // assert
        Assert.AreEqual("Task where bid is 27", query);
    }

    [TestMethod]
    public void Translate_TableWithEqualityConditionOnString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Name == "foobar");

        // assert
        Assert.AreEqual("Task where name is 'foobar'", query);
    }

    [TestMethod]
    public void Translate_TableWithEqualityConditionOnObjectString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Parent.Name == "foobar");

        // assert
        Assert.AreEqual("Task where parent has (name is 'foobar')", query);
    }

    [TestMethod]
    public void Translate_TableWithSubQueryConditionOnArrayObjectString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Children.Any(x => x.Name == "foobar"));

        // assert
        Assert.AreEqual("Task where children any (name is 'foobar')", query);
    }

    [TestMethod]
    public void Translate_TableWithContainsConditionOnArrayString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        List<string> stringArray = ["foo", "bar"];

        // act
        var query = expressionVisitor.Translate((Task x) => stringArray.Contains(x.Name));

        // assert
        Assert.AreEqual("Task where name in ('foo', 'bar')", query);
    }

    [TestMethod]
    public void Translate_TableWithNegatedContainsConditionOnArrayString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        List<string> stringArray = ["foo", "bar"];

        // act
        var query = expressionVisitor.Translate((Task x) => !stringArray.Contains(x.Name));

        // assert
        Assert.AreEqual("Task where name not_in ('foo', 'bar')", query);
    }

    [TestMethod]
    public void Translate_TableWithStartsWithConditionOnString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Name.StartsWith("foobar"));

        // assert
        Assert.AreEqual("Task where name like 'foobar%'", query);
    }

    [TestMethod]
    public void Translate_TableWithEndsWithConditionOnString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Name.EndsWith("foobar"));

        // assert
        Assert.AreEqual("Task where name like '%foobar'", query);
    }

    [TestMethod]
    public void Translate_TableWithContainsConditionOnString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Name.Contains("foobar"));

        // assert
        Assert.AreEqual("Task where name like '%foobar%'", query);
    }
}