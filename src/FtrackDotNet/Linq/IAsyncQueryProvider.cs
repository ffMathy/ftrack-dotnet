using System.Linq.Expressions;

namespace FtrackDotNet.Linq;

internal interface IAsyncQueryProvider : IQueryProvider
{
    Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);
}