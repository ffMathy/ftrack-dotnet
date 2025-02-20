using System.Text.Json;

namespace FtrackDotNet.Api.Responses;

internal class QueryResponseWrapper<T>
{
    public string Action { get; set; }
    public T Data { get; set; }
    public Dictionary<string, JsonElement> Metadata { get; set; }
}