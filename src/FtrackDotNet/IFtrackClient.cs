namespace FtrackDotNet;

public interface IFtrackClient
{
    /// <summary>
    /// Run a query expression against FTrack. Returns raw JSON or a strongly typed object.
    /// For maximum flexibility, we might return dynamic or a custom model. Here we show "List<T>" for example.
    /// </summary>
    List<object> ExecuteQuery(string queryExpression, Type returnType);

    /// <summary>
    /// Async version of ExecuteQuery that posts to /query
    /// and returns the result as a List<T>.
    /// </summary>
    Task<List<object>> ExecuteQueryAsync(string queryExpression, Type returnType);
}