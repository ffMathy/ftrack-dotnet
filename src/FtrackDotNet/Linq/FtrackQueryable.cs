namespace FtrackDotNet.Linq;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

internal class FtrackQueryable<T> : IQueryable<T>, IOrderedQueryable<T>, IAsyncEnumerable<T>
{
    private readonly FtrackQueryProvider _provider;
    private readonly Expression _expression;

    public FtrackQueryable(FtrackQueryProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _expression = Expression.Constant(this);
    }

    public FtrackQueryable(FtrackQueryProvider provider, Expression expression)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public Type ElementType => typeof(T);

    public Expression Expression => _expression;

    public IQueryProvider Provider => _provider;

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        // When iterating asynchronously, we'll let the provider handle
        // how we fetch data from Ftrack and return an enumerator
        var results = await _provider.ExecuteAsync<T>(_expression, cancellationToken);
        throw new NotImplementedException();
        yield return default;
    }

    public IEnumerator<T> GetEnumerator()
    {
        // For synchronous enumeration, we can do a blocking call
        throw new NotImplementedException("Synchronous enumeration of Ftrack entities is not supported.");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}