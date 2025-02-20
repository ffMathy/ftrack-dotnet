using System.Linq.Expressions;
using FtrackDotNet.Linq;

namespace FtrackDotNet.Extensions;

public static class FtrackAsyncExtensions
{
    /// <summary>
    /// Asynchronously materialize all elements of the query into a List&lt;T&gt;.
    /// </summary>
    public static async Task<List<T>> ToListAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        if (source.Provider is IFtrackQueryProvider asyncProvider)
        {
            // Execute the expression asynchronously to get IEnumerable<T>
            var result = await asyncProvider
                .ExecuteAsync<IEnumerable<T>>(source.Expression, cancellationToken)
                .ConfigureAwait(false);

            // Materialize in memory
            return result.ToList();
        }
        else
        {
            // Fallback to synchronous
            return source.ToList();
        }
    }

    /// <summary>
    /// Asynchronously materialize all elements of the query into an array.
    /// </summary>
    public static async Task<T[]> ToArrayAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        var list = await source.ToListAsync(cancellationToken).ConfigureAwait(false);
        return list.ToArray();
    }

    /// <summary>
    /// Asynchronously returns the first element of the sequence.
    /// Throws if the sequence is empty.
    /// </summary>
    public static async Task<T> FirstAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        // We'll emulate "First" by forcing Take(1) then check if empty.
        if (source.Provider is IFtrackQueryProvider asyncProvider)
        {
            // Build a "Take(1)" expression
            var expression = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Take),
                [typeof(T)],
                source.Expression,
                Expression.Constant(1));

            var results = await asyncProvider
                .ExecuteAsync<IEnumerable<T>>(expression, cancellationToken)
                .ConfigureAwait(false);

            using var enumerator = results.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }
            return enumerator.Current;
        }
        else
        {
            // Fallback
            return source.First();
        }
    }

    /// <summary>
    /// Asynchronously returns the first element of the sequence,
    /// or a default value if the sequence is empty.
    /// </summary>
    public static async Task<T> FirstOrDefaultAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        // Similar approach to FirstAsync, but if no elements, return default.
        if (source.Provider is IFtrackQueryProvider asyncProvider)
        {
            var expression = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Take),
                [typeof(T)],
                source.Expression,
                Expression.Constant(1));

            var results = await asyncProvider
                .ExecuteAsync<IEnumerable<T>>(expression, cancellationToken)
                .ConfigureAwait(false);

            using var enumerator = results.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return default!;
            }
            return enumerator.Current;
        }
        else
        {
            // Fallback
            return source.FirstOrDefault();
        }
    }

    /// <summary>
    /// Asynchronously returns the single element of the sequence,
    /// throws if more than one or if none.
    /// </summary>
    public static async Task<T> SingleAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        // Emulate "Single": Take(2), ensure exactly 1 element
        if (source.Provider is IFtrackQueryProvider asyncProvider)
        {
            var expression = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Take),
                [typeof(T)],
                source.Expression,
                Expression.Constant(2));

            var results = await asyncProvider
                .ExecuteAsync<IEnumerable<T>>(expression, cancellationToken)
                .ConfigureAwait(false);

            using var enumerator = results.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            var first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                throw new InvalidOperationException("Sequence contains more than one element");
            }

            return first;
        }
        else
        {
            return source.Single();
        }
    }

    /// <summary>
    /// Asynchronously returns the single element of the sequence,
    /// or default if none. Throws if more than one.
    /// </summary>
    public static async Task<T> SingleOrDefaultAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        if (source.Provider is IFtrackQueryProvider asyncProvider)
        {
            var expression = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Take),
                [typeof(T)],
                source.Expression,
                Expression.Constant(2));

            var results = await asyncProvider
                .ExecuteAsync<IEnumerable<T>>(expression, cancellationToken)
                .ConfigureAwait(false);

            using var enumerator = results.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return default!;
            }

            var first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                throw new InvalidOperationException("Sequence contains more than one element");
            }

            return first;
        }
        else
        {
            return source.SingleOrDefault();
        }
    }

    /// <summary>
    /// Asynchronously returns the last element of the sequence.
    /// Because FTrack doesn't natively do "ORDER BY" + "DESC" + "LIMIT 1" easily (unless you implement it),
    /// we do a naive approach by enumerating all in memory.
    /// </summary>
    public static async Task<T> LastAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        var list = await source.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }
        return list[^1];
    }

    /// <summary>
    /// Asynchronously returns the last element of the sequence, or default if empty.
    /// Naive approach enumerates everything in memory.
    /// </summary>
    public static async Task<T> LastOrDefaultAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        var list = await source.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (list.Count == 0) return default!;
        return list[^1];
    }

    /// <summary>
    /// Asynchronously determines whether any elements satisfy the condition, or just any if no predicate is provided.
    /// We do expression rewriting: "Take(1)" and see if it's empty.
    /// </summary>
    public static async Task<bool> AnyAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        if (source.Provider is IFtrackQueryProvider asyncProvider)
        {
            // .Take(1)
            var expression = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Take),
                [typeof(T)],
                source.Expression,
                Expression.Constant(1));

            var results = await asyncProvider
                .ExecuteAsync<IEnumerable<T>>(expression, cancellationToken)
                .ConfigureAwait(false);

            // If we have at least 1, return true
            return results.GetEnumerator().MoveNext();
        }
        else
        {
            return source.Any();
        }
    }

    /// <summary>
    /// Asynchronously checks if all elements satisfy a predicate. 
    /// We'll rewrite "source.All(predicate)" to "source.Where(!predicate).Take(1).Any() == false".
    /// (This is how EF does it internally.)
    /// </summary>
    public static async Task<bool> AllAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        // "All(p)" means "not any element that fails p"
        // so we do "Where(not p).Take(1).Any()" => if that is true, then it's not "all".
        var negated = Expression.Lambda<Func<T, bool>>(
            Expression.Not(predicate.Body),
            predicate.Parameters
        );
        
        IQueryable<T> negatedQuery = source.Where(negated);
        var failsPredicate = await negatedQuery.AnyAsync(cancellationToken).ConfigureAwait(false);
        return !failsPredicate;
    }

    /// <summary>
    /// Asynchronously returns the number of elements in the sequence.
    /// Naive approach enumerates all items and counts them.
    /// If FTrack can do a server-side count, consider an expression-based approach.
    /// </summary>
    public static async Task<int> CountAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        if (source.Provider is IFtrackQueryProvider asyncProvider)
        {
            // Naive approach: get all, then count in memory
            var results = await asyncProvider
                .ExecuteAsync<IEnumerable<T>>(source.Expression, cancellationToken)
                .ConfigureAwait(false);
            return results.Count();
        }
        else
        {
            return source.Count();
        }
    }

    /// <summary>
    /// Asynchronously returns the number of elements in the sequence as a long (int64).
    /// Same naive approach enumerating all items.
    /// </summary>
    public static async Task<long> LongCountAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        if (source.Provider is IFtrackQueryProvider asyncProvider)
        {
            var results = await asyncProvider
                .ExecuteAsync<IEnumerable<T>>(source.Expression, cancellationToken)
                .ConfigureAwait(false);
            return results.LongCount();
        }
        else
        {
            return source.LongCount();
        }
    }

    /// <summary>
    /// Asynchronously checks whether the sequence contains a given item.
    /// Typically does in-memory check after retrieving results,
    /// or you could rewrite to "Where(x => x == item).Take(1).Any()".
    /// 
    /// For reference types, you may need an EqualityComparer or 
    /// custom logic to match FTrack fields. 
    /// This is a naive approach.
    /// </summary>
    public static async Task<bool> ContainsAsync<T>(
        this IQueryable<T> source,
        T item,
        CancellationToken cancellationToken = default)
    {
        // We can do a quick expression rewrite: "Where(x => x == item).Take(1).Any()"
        // But that depends on how your translator handles "x => x == item".
        // For simplicity, let's do naive in-memory approach:
        var all = await source.ToListAsync(cancellationToken).ConfigureAwait(false);
        return all.Contains(item);
    }

    /// <summary>
    /// Asynchronously returns the minimum value by enumerating in memory.
    /// If FTrack supports a 'min' function, consider specialized expression rewriting.
    /// </summary>
    public static async Task<T> MinAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        var all = await source.ToListAsync(cancellationToken);
        if (all.Count == 0) throw new InvalidOperationException("Sequence was empty.");
        return all.Min();
    }

    /// <summary>
    /// Asynchronously returns the maximum value by enumerating in memory.
    /// If FTrack supports 'max', consider specialized expression rewriting.
    /// </summary>
    public static async Task<T> MaxAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
    {
        var all = await source.ToListAsync(cancellationToken);
        if (all.Count == 0) throw new InvalidOperationException("Sequence was empty.");
        return all.Max();
    }

    /// <summary>
    /// Asynchronously computes the sum in memory.
    /// If you want partial sums at the server, consider expression rewriting if supported.
    /// </summary>
    public static async Task<decimal> SumAsync(
        this IQueryable<decimal> source,
        CancellationToken cancellationToken = default)
    {
        // Overloads for different numeric types (int, long, double, float, decimal, etc.) 
        // might also be defined. This is just one example.
        var all = await source.ToListAsync(cancellationToken);
        return all.Sum();
    }

    /// <summary>
    /// Asynchronously computes the average in memory.
    /// </summary>
    public static async Task<decimal> AverageAsync(
        this IQueryable<decimal> source,
        CancellationToken cancellationToken = default)
    {
        var all = await source.ToListAsync(cancellationToken);
        if (all.Count == 0) throw new InvalidOperationException("Sequence contains no elements");
        return all.Average();
    }

    // In a real library, you'd replicate SumAsync / AverageAsync 
    // for int, double, float, long, decimal?, etc., just like LINQ does.
}