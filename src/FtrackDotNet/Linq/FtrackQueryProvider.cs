using System;
using System.Linq;
using System.Linq.Expressions;

namespace FtrackDotNet.Linq;

public class FtrackQueryProvider : IQueryProvider, IAsyncQueryProvider
{
    private readonly IFtrackClient _client;

    public FtrackQueryProvider(IFtrackClient client)
    {
        _client = client;
    }

    // ---------- Synchronous IQueryProvider Members ----------

    public IQueryable CreateQuery(Expression expression)
    {
        Type elementType = TypeSystem.GetElementType(expression.Type);
        return (IQueryable)Activator.CreateInstance(
            typeof(FtrackQueryable<>).MakeGenericType(elementType),
            this, 
            expression
        );
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new FtrackQueryable<TElement>(this);
    }

    public object Execute(Expression expression)
    {
        return Execute<object>(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        var queryString = BuildFtrackQuery(expression);
        var result = _client.ExecuteQuery(queryString, typeof(TResult));
        return (TResult)(object)result;
    }

    // ---------- Asynchronous IAsyncQueryProvider Member ----------

    public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var queryString = BuildFtrackQuery(expression);
        return (TResult)(object)await _client.ExecuteQueryAsync(queryString, typeof(TResult));
    }

    // ---------- Helper to Build Query String ----------
    private string BuildFtrackQuery(Expression expression)
    {    
        var visitor = new FtrackExpressionVisitor("Task");
        return visitor.Translate(expression);
    }
}