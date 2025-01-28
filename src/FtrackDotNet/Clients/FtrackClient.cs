using Microsoft.Extensions.Options;

namespace FtrackDotNet;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

internal class FtrackClient : IDisposable, IFtrackClient
{
    private readonly HttpClient _http;

    /// <summary>
    /// Create a new FtrackClient with the given options.
    /// Typically you pass in a HttpClientFactory in real apps, 
    /// but for brevity we'll create an HttpClient here.
    /// </summary>
    public FtrackClient(IOptionsSnapshot<FtrackContextOptions> options)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(options.Value.ServerUrl, UriKind.Absolute),
            // Optionally set timeouts, etc.
        };

        // According to FTrack docs, you can authenticate by setting headers
        // e.g. "X-Ftrack-User" and "X-Ftrack-ApiKey"
        // or "Authorization: Bearer <token>" for personal tokens
        //
        // We'll assume user+API key approach:
        _http.DefaultRequestHeaders.Add("Ftrack-User", options.Value.ApiUser);
        _http.DefaultRequestHeaders.Add("Ftrack-Api-Key", options.Value.ApiKey);

        // If your usage requires a different scheme, adapt accordingly.
        // e.g. "Authorization: Bearer ..."
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

    public async Task<T> QueryAsync<T>(string query)
    {
        return await CallAsync<Dictionary<string, object>, T>(
            new Dictionary<string, object>
            {
                { "action", "query" },
                { "expression", query }
            });
    }

    public async Task<QuerySchemasSchemaResponse> QuerySchemasAsync()
    {
        return await CallAsync<Dictionary<string, object>, QuerySchemasSchemaResponse>(
            new Dictionary<string, object>
            {
                { "action", "query_schemas" },
            });
    }

    private async Task<TResponse> CallAsync<TRequest, TResponse>(TRequest request)
    {
        var json = JsonSerializer.Serialize(new[] { request });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("api", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        var wrappedReturnType = typeof(QueryResponseWrapper<>).MakeGenericType(typeof(TResponse));
        var result = (QueryResponseWrapper<TResponse>[])JsonSerializer.Deserialize(
            responseBody,
            wrappedReturnType.MakeArrayType(),
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            })!;

        return result.Select(x => x.Data).Single();
    }
}

//TODO: convert into subtyped classes