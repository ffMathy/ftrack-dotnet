using System.Collections;
using System.Linq.Expressions;
using System.Text.Json;
using FtrackDotNet.Api;
using FtrackDotNet.Linq.Visitors;
using FtrackDotNet.Models;
using FtrackDotNet.UnitOfWork;
using Type = System.Type;

namespace FtrackDotNet.Linq;

internal class FtrackQueryProvider(
    IFtrackClient client,
    IChangeTracker changeTracker) : IFtrackQueryProvider
{
    private readonly IFtrackClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly FtrackExpressionVisitor _visitor = new();

    private bool SkipTracking { get; init; }

    public IQueryable CreateQuery(Expression expression)
    {
        // Return a non-generic IQueryable
        var elementType = GetElementTypeFromExpression(expression);
        var queryableType = typeof(FtrackQueryable<>).MakeGenericType(elementType);
        return (IQueryable) Activator.CreateInstance(queryableType, this, expression)!;
    }

    private static Type GetElementTypeFromExpression(Expression expression)
    {
        var visitor = new FtrackFromExpressionVisitor();
        visitor.Visit(expression);

        return visitor.Type;
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new FtrackQueryable<TElement>(this, expression);
    }

    public object Execute(Expression expression)
    {
        // For synchronous calls
        throw new InvalidOperationException("Only asynchronous calls are supported.");
    }

    public TResult Execute<TResult>(Expression expression)
    {
        // For synchronous calls
        throw new InvalidOperationException("Only asynchronous calls are supported.");
    }

    // The async path
    public async Task<TResult> ExecuteAsync<TResult>(
        Expression expression,
        CancellationToken cancellationToken)
    {
        // 1. Visit expression tree -> get a FtrackQueryDefinition
        var query = _visitor.Translate(expression);
        var elementType = GetElementTypeFromExpression(expression);

        // 2. Call into the IFtrackClient with the query definition
        var jsonResults = await _client.QueryAsync(query, cancellationToken);

        var jsonElements = jsonResults.Single();
        if (jsonElements.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Expected result from Ftrack to be an array, but got: " +
                                                jsonElements.ValueKind);
        }

        if(typeof(TResult) is not { IsGenericType: true, GenericTypeArguments: [var genericType] })
        {
            throw new InvalidOperationException("Expected TResult to be a generic type.");
        }

        var results = (IList) Activator.CreateInstance(
            typeof(List<>).MakeGenericType(genericType),
            new object?[]
            {
                jsonElements.GetArrayLength()
            })!;
        foreach (var jsonElement in jsonElements.EnumerateArray())
        {
            if (SkipTracking)
            {
                continue;
            }

            var realObject = jsonElement.Deserialize(genericType, FtrackContext.GetJsonSerializerOptions());
            if (realObject == null)
            {
                throw new InvalidOperationException("Failed to deserialize object.");
            }
            
            changeTracker.TrackEntity(
                jsonElement,
                realObject,
                TrackedEntityOperationType.Update);
            results.Add(realObject);
        }

        return (TResult)results;
    }

    public IFtrackQueryProvider AsNoTracking()
    {
        return new FtrackQueryProvider(
            _client,
            changeTracker)
        {
            SkipTracking = true
        };
    }
}