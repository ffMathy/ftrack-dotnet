using System.Linq.Expressions;

namespace FtrackDotNet.Linq;

public interface IFtrackQueryProvider : IQueryProvider
{
    Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);

    IFtrackQueryProvider AsNoTracking();
}