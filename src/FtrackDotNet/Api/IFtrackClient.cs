using System.Text.Json;
using System.Text.Json.Serialization;

namespace FtrackDotNet.Api;

public interface IFtrackClient
{
    Task<JsonElement[]> QueryAsync(string query, CancellationToken cancellationToken = default);
    Task<JsonElement[]> CallAsync(IEnumerable<FtrackOperation> operations, CancellationToken cancellationToken = default);
    
    Task<QuerySchemasSchemaResponse[]> QuerySchemasAsync(CancellationToken cancellationToken = default);
    
    Task<string> MakeRawRequestAsync(HttpMethod method, string relativeUrl, string? json = null, CancellationToken cancellationToken = default);
}