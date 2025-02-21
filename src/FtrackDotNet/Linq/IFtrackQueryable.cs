namespace FtrackDotNet.Linq;

public interface IFtrackQueryable<out T> : IQueryable<T>, IAsyncEnumerable<T> {}