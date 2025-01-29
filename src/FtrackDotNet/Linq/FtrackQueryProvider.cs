using System.Linq.Expressions;
using FtrackDotNet.Clients;
using FtrackDotNet.UnitOfWork;

namespace FtrackDotNet.Linq;

internal class FtrackQueryProvider(
    IFtrackClient client,
    IFtrackTransactionState ftrackTransactionState) : IQueryProvider, IAsyncQueryProvider
{
    private readonly IFtrackClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly FtrackExpressionVisitor _visitor = new FtrackExpressionVisitor();

    public IQueryable CreateQuery(Expression expression)
    {
        // Return a non-generic IQueryable
        var elementType = expression.Type.GetGenericArguments().First();
        var queryableType = typeof(FtrackQueryable<>).MakeGenericType(elementType);
        return (IQueryable)Activator.CreateInstance(queryableType, this, expression);
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

        // 2. Call into the IFtrackClient with the query definition
        var results = await _client.QueryAsync<TResult>(query);
        foreach(var result in (IEnumerable<object>)results!)
        {
            ftrackTransactionState.CurrentTransaction.Value?.ChangeDetector.TrackEntity(result);
        }

        return results;
    }
}