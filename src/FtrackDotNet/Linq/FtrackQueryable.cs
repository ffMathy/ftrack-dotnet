namespace FtrackDotNet.Linq;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class FtrackQueryable<T> : IFtrackQueryable<T>
{
    private readonly IFtrackQueryProvider _provider;
    private readonly Expression _expression;

    public FtrackQueryable(IFtrackQueryProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _expression = Expression.Constant(this);
    }

    public FtrackQueryable(IFtrackQueryProvider provider, Expression expression)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public IFtrackQueryable<T> AsNoTracking()
    {
        return new FtrackQueryable<T>(
            _provider.AsNoTracking(),
            _expression);
    }

    public Type ElementType => typeof(T);

    public Expression Expression => _expression;

    public IQueryProvider Provider => _provider;

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var results = await _provider.ExecuteAsync<IEnumerable<T>>(_expression, cancellationToken);
        foreach (var result in results)
        {
            yield return result;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        throw new InvalidOperationException(
            "Synchronous iteration of Ftrack results is not supported, as it is discouraged.");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}