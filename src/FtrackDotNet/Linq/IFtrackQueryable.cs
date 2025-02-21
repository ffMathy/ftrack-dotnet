namespace FtrackDotNet.Linq;

public interface IFtrackQueryable<T> : IQueryable<T>, IOrderedQueryable<T>, IAsyncEnumerable<T> {}