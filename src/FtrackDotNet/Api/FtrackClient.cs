using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace FtrackDotNet.Api;

internal class FtrackClient : IDisposable, IFtrackClient
{
    private readonly HttpClient _http;

    public FtrackClient(
        IOptionsMonitor<FtrackOptions> options)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(options.CurrentValue.ServerUrl, UriKind.Absolute),
        };

        _http.DefaultRequestHeaders.Add("Ftrack-User", options.CurrentValue.ApiUser);
        _http.DefaultRequestHeaders.Add("Ftrack-Api-Key", options.CurrentValue.ApiKey);
    }

    /// <summary>
    /// Dispose for our HttpClient if needed.
    /// In a real production scenario, you might rely on HttpClientFactory or
    /// not dispose it as frequently.
    /// </summary>
    public void Dispose()
    {
        _http.Dispose();
    }

    public async Task<T[]> QueryAsync<T>(string query, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Querying: {query}");
        return await CallAsync<T>([
            new FtrackQueryOperation()
            {
                Expression = query
            }
        ]);
    }

    public async Task<T[]> CallAsync<T>(IEnumerable<FtrackOperation> operations, CancellationToken cancellationToken = default)
    {
        var result = await MakeApiRequestAsync<QueryResponseWrapper<T>[]>(operations);
        return result
            .Select(x => x.Data)
            .ToArray()!;
    }

    public async Task<QuerySchemasSchemaResponse[]> QuerySchemasAsync(CancellationToken cancellationToken = default)
    {
        return await CallAsync<QuerySchemasSchemaResponse>(
            [new FtrackQuerySchemasOperation()], 
            cancellationToken);
    }

    private async Task<TResponse> MakeApiRequestAsync<TResponse>(object request)
    {
        var json = JsonSerializer.Serialize(
            request,
            GetJsonSerializerOptions());

        var responseBody = await MakeRawRequestAsync(HttpMethod.Post, "api", json);

        var result = JsonSerializer.Deserialize<JsonElement>(
            responseBody,
            GetJsonSerializerOptions());

        if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("exception", out _))
        {
            var response = result.Deserialize<FtrackServerErrorResponse>(GetJsonSerializerOptions())!;
            throw new FtrackServerException(response!);
        }

        return (TResponse)result.Deserialize(typeof(TResponse), GetJsonSerializerOptions())!;
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<string> MakeRawRequestAsync(HttpMethod method, string relativeUrl, string? json = null, CancellationToken cancellationToken = default)
    {
        var content = json != null ? 
            new StringContent(json, Encoding.UTF8, "application/json") : 
            null;

        var message = new HttpRequestMessage(method, relativeUrl)
        {
            Content = content
        };
        var response = await _http.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody;
    }
}

[JsonPolymorphic]
[JsonDerivedType(typeof(FtrackQuerySchemasOperation))]
[JsonDerivedType(typeof(FtrackQueryOperation))]
[JsonDerivedType(typeof(FtrackCreateOperation))]
[JsonDerivedType(typeof(FtrackUpdateOperation))]
[JsonDerivedType(typeof(FtrackDeleteOperation))]
public abstract class FtrackOperation
{
    public abstract string Action { get; }
    public object Metadata { get; init; } = new();
}

public class FtrackQuerySchemasOperation : FtrackOperation
{
    public override string Action => "query_schemas";
}

public class FtrackQueryOperation : FtrackOperation
{
    public override string Action => "query";
    public required string Expression { get; init; }
}

public class FtrackCreateOperation : FtrackOperation
{
    public override string Action => "create";
    public required string EntityType { get; init; }
    public required object EntityData { get; init; }
}

public class FtrackUpdateOperation : FtrackOperation
{
    public override string Action => "update";
    public required string EntityType { get; init; }
    public required object EntityKey { get; init; }
    public required object EntityData { get; init; }
}

public class FtrackDeleteOperation : FtrackOperation
{
    public override string Action => "delete";
    public required string EntityType { get; init; }
    public required object EntityKey { get; init; }
}