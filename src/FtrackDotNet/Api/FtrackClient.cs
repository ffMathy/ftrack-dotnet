using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FtrackDotNet.Api.Models;
using FtrackDotNet.Api.Requests;
using FtrackDotNet.Api.Requests.Operations;
using FtrackDotNet.Api.Responses;
using FtrackDotNet.UnitOfWork;
using Microsoft.Extensions.Options;

namespace FtrackDotNet.Api;

internal class FtrackClient : IFtrackClient
{
    private readonly HttpClient _http;

    public FtrackClient(
        IOptionsMonitor<FtrackOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient();
        _http.BaseAddress = new Uri(options.CurrentValue.ServerUrl, UriKind.Absolute);
        _http.Timeout = options.CurrentValue.RequestTimeout ?? Timeout.InfiniteTimeSpan;

        _http.DefaultRequestHeaders.Add("Ftrack-User", options.CurrentValue.ApiUser);
        _http.DefaultRequestHeaders.Add("Ftrack-Api-Key", options.CurrentValue.ApiKey);
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

        return await MakeRawApiRequestAsync<TResponse>(requestJson, cancellationToken);
    }

    private async Task<TResponse> MakeRawApiRequestAsync<TResponse>(string requestJson, CancellationToken cancellationToken)
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
