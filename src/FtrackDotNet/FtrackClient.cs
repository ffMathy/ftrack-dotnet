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
        // Build the request payload, e.g.:
        // { "expression": "Task where status.name is \"Open\" limit 10 offset 5", "page_size": 9999 }
        var payload = new Dictionary<string, object>
        {
            { "action", "query" },
            { "expression", query }
        };

        // Convert to JSON
        var json = JsonSerializer.Serialize(new[] { payload });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // POST to /query
        var response = await _http.PostAsync("api", content);

        response.EnsureSuccessStatusCode(); // throws if not 200-299

        var responseBody = await response.Content.ReadAsStringAsync();

        // FTrack typically returns JSON in a structure like:
        // {
        //   "data": [ { ... }, { ... }, ... ],
        //   "metadata": { ... }
        // }
        // We'll define a helper model to parse it, then map "data" to List<T>.

        var wrappedReturnType = typeof(QueryResponseWrapper<>).MakeGenericType(typeof(T));
        var result = (QueryResponseWrapper<T>[])JsonSerializer.Deserialize(
            responseBody,
            wrappedReturnType.MakeArrayType(),
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })!;

        // "data" property in result.Data is a List<T>
        // but only if T's shape aligns with the JSON. In many cases, 
        // you may want to parse as dynamic or a dictionary, then map to T manually.
        // For demonstration, we assume T matches the structure.

        return result.Select(x => x.Data).Single();
    }
}

/// <summary>
/// A helper model to map FTrack's query response JSON.
/// For example, the docs show something like:
/// {
///   "data": [ { "id": "xxx", ... }, ... ],
///   "metadata": { ... }
/// }
/// We assume T lines up with each item in "data".
/// </summary>
/// <typeparam name="T">The type representing each row/item returned by FTrack.</typeparam>
public class QueryResponseWrapper<T>
{
    public string Action { get; set; }
    public T Data { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}