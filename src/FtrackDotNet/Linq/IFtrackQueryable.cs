namespace FtrackDotNet.Linq;

public interface IFtrackQueryable<out T> : IQueryable<T>, IOrderedQueryable<T>, IAsyncEnumerable<T> {}