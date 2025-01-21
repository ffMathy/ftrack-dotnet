namespace FtrackDotNet;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public class FtrackClient : IDisposable
{
    private readonly HttpClient _http;

    /// <summary>
    /// Create a new FtrackClient with the given options.
    /// Typically you pass in a HttpClientFactory in real apps, 
    /// but for brevity we'll create an HttpClient here.
    /// </summary>
    public FtrackClient(FtrackContextOptions options)
    {
        // Build a base HttpClient
        _http = new HttpClient
        {
            BaseAddress = new Uri(options.ServerUrl, UriKind.Absolute),
            // Optionally set timeouts, etc.
        };

        // According to FTrack docs, you can authenticate by setting headers
        // e.g. "X-Ftrack-User" and "X-Ftrack-ApiKey"
        // or "Authorization: Bearer <token>" for personal tokens
        //
        // We'll assume user+API key approach:
        _http.DefaultRequestHeaders.Add("X-Ftrack-User", options.ApiUser);
        _http.DefaultRequestHeaders.Add("X-Ftrack-ApiKey", options.ApiKey);

        // If your usage requires a different scheme, adapt accordingly.
        // e.g. "Authorization: Bearer ..."
    }

    /// <summary>
    /// Run a query expression against FTrack. Returns raw JSON or a strongly typed object.
    /// For maximum flexibility, we might return dynamic or a custom model. Here we show "List<T>" for example.
    /// </summary>
    public List<T> ExecuteQuery<T>(string queryExpression, int? pageSize = null, string sort = null)
    {
        // Synchronously wait on the async method (not best practice in real .NET apps),
        // but for sample simplicity we'll do it. 
        // Real code might do "await" in an async method.
        return ExecuteQueryAsync<T>(queryExpression, pageSize, sort).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async version of ExecuteQuery that posts to /query
    /// and returns the result as a List<T>.
    /// </summary>
    public async Task<List<T>> ExecuteQueryAsync<T>(string queryExpression, int? pageSize = null, string sort = null)
    {
        // Build the request payload, e.g.:
        // { "expression": "Task where status.name is \"Open\" limit 10 offset 5", "page_size": 9999 }
        var payload = new Dictionary<string, object>
        {
            { "expression", queryExpression }
        };
        if (pageSize.HasValue)
        {
            payload["page_size"] = pageSize.Value;
        }
        if (!string.IsNullOrWhiteSpace(sort))
        {
            payload["sort"] = sort;  // e.g. "name ascending"
        }

        // Convert to JSON
        string json = JsonSerializer.Serialize(payload);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // POST to /query
        var response = await _http.PostAsync("query", content);

        response.EnsureSuccessStatusCode(); // throws if not 200-299

        string responseBody = await response.Content.ReadAsStringAsync();

        // FTrack typically returns JSON in a structure like:
        // {
        //   "data": [ { ... }, { ... }, ... ],
        //   "metadata": { ... }
        // }
        // We'll define a helper model to parse it, then map "data" to List<T>.

        var result = JsonSerializer.Deserialize<QueryResponseWrapper<T>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // "data" property in result.Data is a List<T>
        // but only if T's shape aligns with the JSON. In many cases, 
        // you may want to parse as dynamic or a dictionary, then map to T manually.
        // For demonstration, we assume T matches the structure.

        return result.Data;
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
    public List<T> Data { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}