using System.Text.Json;

namespace FtrackDotNet.Clients;

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
internal class QueryResponseWrapper<T>
{
    public string Action { get; set; }
    public T Data { get; set; }
    public Dictionary<string, JsonElement> Metadata { get; set; }
}