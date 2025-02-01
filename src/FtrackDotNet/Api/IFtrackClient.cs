namespace FtrackDotNet.Api;

public interface IFtrackClient
{
    Task<T[]> QueryAsync<T>(string query, CancellationToken cancellationToken = default);
    Task<QuerySchemasSchemaResponse[]> QuerySchemasAsync(CancellationToken cancellationToken = default);
    Task<T[]> CallAsync<T>(IEnumerable<FtrackOperation> operations, CancellationToken cancellationToken = default);
    Task<string> MakeRawRequestAsync(HttpMethod method, string relativeUrl, string? json = null, CancellationToken cancellationToken = default);
}