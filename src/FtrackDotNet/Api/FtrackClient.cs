using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FtrackDotNet.UnitOfWork;
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

    public async Task<JsonElement[]> QueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Querying: {query}");
        return await CallAsync(
            [
                new FtrackQueryOperation()
                {
                    Expression = query
                }
            ],
            cancellationToken);
    }

    public async Task<JsonElement[]> CallAsync(
        IEnumerable<FtrackOperation> operations,
        CancellationToken cancellationToken = default)
    {
        var result =
            await MakeApiRequestAsync<QueryResponseWrapper<JsonElement>[]>(
                operations,
                cancellationToken);
        return result
            .Select(x => x.Data)
            .ToArray()!;
    }

    public async Task<QuerySchemasSchemaResponse[]> QuerySchemasAsync(CancellationToken cancellationToken = default)
    {
        var result = await MakeApiRequestAsync<QuerySchemasSchemaResponse[][]>(
            new FtrackOperation[] {new FtrackQuerySchemasOperation()},
            cancellationToken);
        return result.Single();
    }

    private async Task<TResponse> MakeApiRequestAsync<TResponse>(
        object request,
        CancellationToken cancellationToken = default)
    {
        var requestJson = JsonSerializer.Serialize(
            request,
            FtrackContext.GetJsonSerializerOptions());

        return await MakeJsonApiRequestAsync<TResponse>(requestJson, cancellationToken);
    }

    private async Task<TResponse> MakeJsonApiRequestAsync<TResponse>(string requestJson, CancellationToken cancellationToken)
    {
        var responseBody = await MakeRawRequestAsync(HttpMethod.Post, "api", requestJson, cancellationToken);

        var result = JsonSerializer.Deserialize<JsonElement>(
            responseBody,
            FtrackContext.GetJsonSerializerOptions());

        if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("exception", out _))
        {
            var response = result.Deserialize<FtrackServerErrorResponse>(FtrackContext.GetJsonSerializerOptions())!;
            throw new FtrackServerException(requestJson, response!);
        }

        return (TResponse) result.Deserialize(typeof(TResponse), FtrackContext.GetJsonSerializerOptions())!;
    }

    public async Task<string> MakeRawRequestAsync(HttpMethod method, string relativeUrl, string? json = null,
        CancellationToken cancellationToken = default)
    {
        var content = json != null ? new StringContent(json, Encoding.UTF8, "application/json") : null;

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
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required object EntityData { get; init; }
}

public class FtrackUpdateOperation : FtrackOperation
{
    public override string Action => "update";
    public required string EntityType { get; init; }
    public required object EntityKey { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required object EntityData { get; init; }
}

public class FtrackDeleteOperation : FtrackOperation
{
    public override string Action => "delete";
    public required string EntityType { get; init; }
    public required object EntityKey { get; init; }
}