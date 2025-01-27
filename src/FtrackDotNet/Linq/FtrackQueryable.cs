namespace FtrackDotNet.Linq;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;

public class FtrackQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>
{
    private readonly Expression _expression;
    private readonly IAsyncQueryProvider _provider; // note: we want an IAsyncQueryProvider ref

    public FtrackQueryable(IAsyncQueryProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _expression = Expression.Constant(this);
    }

    // IQueryable<T> members
    public Type ElementType => typeof(T);
    public Expression Expression => _expression;
    public IQueryProvider Provider => (IQueryProvider)_provider;

    // IEnumerable<T> (sync) fallback
    public IEnumerator<T> GetEnumerator()
    {
        // This calls the sync version if we want to do a synchronous iteration (rare, but possible).
        var enumerable = _provider.Execute<IEnumerable<T>>(_expression);
        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // IAsyncEnumerable<T> for async iteration
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        // We'll call the async provider to get an IEnumerable<T> or List<T> asynchronously
        var results = await _provider.ExecuteAsync<IEnumerable<T>>(_expression, cancellationToken);

        // Then yield return each item
        foreach (var item in results)
        {
            yield return item;
        }
    }
}