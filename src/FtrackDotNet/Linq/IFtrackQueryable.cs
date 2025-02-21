namespace FtrackDotNet.Linq;

// ReSharper disable once RedundantExtendsListEntry
public interface IFtrackQueryable<out T> : IQueryable<T>, IOrderedQueryable<T>, IAsyncEnumerable<T> {}