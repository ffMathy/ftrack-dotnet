using System.Linq.Expressions;

namespace FtrackDotNet.Linq;

public interface IAsyncQueryProvider : IQueryProvider
{
    Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);
}