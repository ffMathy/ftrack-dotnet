using System.Linq.Expressions;
using FtrackDotNet.Api;
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
        return (IQueryable)Activator.CreateInstance(queryableType, this, expression)!;
    }

    private static Type GetElementTypeFromExpression(Expression expression)
    {
        return expression.Type.GetGenericArguments().First();
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
        var results = await _client.QueryAsync<TResult>(query, cancellationToken);
        TrackFetchedEntities(results, elementType);
        return results.Single();
    }

    private void TrackFetchedEntities(object results, Type elementType)
    {
        if (SkipTracking)
        {
            return;
        }

        if (results is IEnumerable<dynamic> enumerable)
        {
            foreach (var result in enumerable)
            {
                TrackFetchedEntities(result, result.GetType());
            }
        } else {
            changeTracker.TrackEntity(results, elementType.Name, TrackedEntityOperationType.Update);
        }
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