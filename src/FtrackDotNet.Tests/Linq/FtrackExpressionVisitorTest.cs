
using System.Collections;
using System.Dynamic;
using FtrackDotNet.Linq;
using NSubstitute;

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
        // Arrange
        var mockClient = Substitute.For<IFtrackClient>();
        
        // We can specify what the mock returns when ExecuteQuery<Task>(...) is called:
        mockClient
            .ExecuteQuery(Arg.Any<string>(), Arg.Any<Type>())
            .Returns(callInfo =>
            {
                var typeUsed = (Type)callInfo.Args()[1];
                var innerType = typeUsed.GetGenericArguments()[0];

                var instance = new { Name = "TestName", Bid = 15.0 };
                var listType = typeof(List<>).MakeGenericType(innerType);
                var list = (IList)Activator.CreateInstance(listType);
                list.Add(instance);
                return list;
            });

        // Create the context with the mock client
        var queryable = new FtrackQueryable<Task>(new FtrackQueryProvider(mockClient));

        // Act
        var result = queryable
            .Where(t => t.Bid > 10)
            .Skip(5)
            .Take(10)
            .Select(t => new { t.Name, t.Bid })
            .ToArray();

        // Assert
        // Check that mockClient.ExecuteQuery<Task>(...) was called exactly once
        mockClient.Received(1).ExecuteQuery(Arg.Any<string>(), Arg.Any<Type>());
    }

    [TestMethod]
    public void Translate_TableWithEqualityConditionOnNumber_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Bid == 27);

        // assert
        Assert.AreEqual("select id from Task where (bid is 27)", query);
    }

    [TestMethod]
    public void Translate_TableWithEqualityConditionOnString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Name == "foobar");

        // assert
        Assert.AreEqual("select id from Task where (name is 'foobar')", query);
    }

    [TestMethod]
    public void Translate_AdvancedHighwayTest_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) =>
            (x.Name == "foobar" || x.Bid > 20) &&
            x.Parent.Parent.Parent.Bid == 400 &&
            x.Children.Any(x => 
                x.Parent.Name == "foobar" || 
                (x.Name == "foobar" && x.Bid > 42) ||
                x.Children.Any(x => x.Bid == 42)));

        // assert
        Assert.AreEqual("select id from Task where ((name is 'foobar' or bid > 20) and parent.parent.parent.bid is 400 and children any (parent.name is 'foobar' or (name is 'foobar' and bid > 42) or children any (bid is 42)))", query);
    }

    [TestMethod]
    public void Translate_TableWithEqualityConditionOnObjectString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Parent.Name == "foobar");

        // assert
        Assert.AreEqual("select id from Task where (parent.name is 'foobar')", query);
    }

    [TestMethod]
    public void Translate_TableWithSubQueryConditionOnArrayObjectString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Children.Any(x => x.Name == "foobar"));

        // assert
        Assert.AreEqual("select id from Task where (children any (name is 'foobar'))", query);
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
        Assert.AreEqual("select id from Task where name in ('foo', 'bar')", query);
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
        Assert.AreEqual("select id from Task where name not_in ('foo', 'bar')", query);
    }

    [TestMethod]
    public void Translate_TableWithStartsWithConditionOnString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Name.StartsWith("foobar"));

        // assert
        Assert.AreEqual("select id from Task where name like 'foobar%'", query);
    }

    [TestMethod]
    public void Translate_TableWithEndsWithConditionOnString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Name.EndsWith("foobar"));

        // assert
        Assert.AreEqual("select id from Task where name like '%foobar'", query);
    }

    [TestMethod]
    public void Translate_TableWithContainsConditionOnString_ReturnsCorrectQuery()
    {
        // arrange
        var expressionVisitor = new FtrackExpressionVisitor("Task");

        // act
        var query = expressionVisitor.Translate((Task x) => x.Name.Contains("foobar"));

        // assert
        Assert.AreEqual("select id from Task where name like '%foobar%'", query);
    }
}