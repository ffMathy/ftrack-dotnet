using System;
using System.Linq;
using System.Linq.Expressions;

namespace FtrackDotNet.Linq;

public class FtrackQueryProvider : IQueryProvider, IAsyncQueryProvider
{
    private readonly FtrackContextOptions _options;
    private readonly FtrackClient _client;

    public FtrackQueryProvider(FtrackContextOptions options)
    {
        _options = options;
        _client = new FtrackClient(_options);
    }

    // ---------- Synchronous IQueryProvider Members ----------

    public IQueryable CreateQuery(Expression expression)
    {
        Type elementType = TypeSystem.GetElementType(expression.Type);
        return (IQueryable)Activator.CreateInstance(
            typeof(FtrackQueryable<>).MakeGenericType(elementType),
            this, expression
        );
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new FtrackQueryable<TElement>(this, expression);
    }

    public object Execute(Expression expression)
    {
        return Execute<object>(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        // Existing synchronous approach
        // Possibly call _client.ExecuteQuery<TResult>(...) 
        // after building the query string.
        var queryString = BuildFtrackQuery(expression);
        return (TResult)(object)_client.ExecuteQuery<TResult>(queryString);
    }

    // ---------- Asynchronous IAsyncQueryProvider Member ----------

    public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        // 1. Build the Ftrack query string from the expression
        string queryString = BuildFtrackQuery(expression);

        // 2. Call the client's async method
        //    Depending on your FtrackClient design, you might pass cancellationToken, 
        //    or handle it differently.
        return (TResult)(object)await _client.ExecuteQueryAsync<TResult>(queryString);
    }

    // ---------- Helper to Build Query String ----------
    private string BuildFtrackQuery(Expression expression)
    {
        var visitor = new FtrackExpressionVisitor("Task"); // or discover entity from expression type
        return visitor.Translate(expression);
    }
}