using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FtrackDotNet.Clients;

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
        };

        _http.DefaultRequestHeaders.Add("Ftrack-User", options.Value.ApiUser);
        _http.DefaultRequestHeaders.Add("Ftrack-Api-Key", options.Value.ApiKey);
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
        var result = await CallAsync<Dictionary<string, object>, QueryResponseWrapper<T>[]>(
            new Dictionary<string, object>
            {
                { "action", "query" },
                { "expression", query }
            });
        return result.Select(x => x.Data).Single();
    }

    public async Task<QuerySchemasSchemaResponse[]> QuerySchemasAsync()
    {
        var result = await CallAsync<Dictionary<string, object>, QuerySchemasSchemaResponse[][]>(
            new Dictionary<string, object>
            {
                { "action", "query_schemas" },
            });
        return result.Single();
    }

    private async Task<TResponse> CallAsync<TRequest, TResponse>(TRequest request)
    {
        var json = JsonSerializer.Serialize(new[] { request });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("api", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        var result = (TResponse)JsonSerializer.Deserialize(
            responseBody,
            typeof(TResponse),
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            })!;

        return result;
    }
}