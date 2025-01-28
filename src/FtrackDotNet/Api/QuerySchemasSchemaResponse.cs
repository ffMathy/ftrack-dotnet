using System.Text.Json;
using System.Text.Json.Serialization;

namespace FtrackDotNet.Clients;

public class QuerySchemasSchemaResponse
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string[] DefaultProjections { get; set; } = Array.Empty<string>();
    public string[] Immutable { get; set; } = Array.Empty<string>();
    public string[] PrimaryKey { get; set; } = Array.Empty<string>();
    public string[] Required { get; set; } = Array.Empty<string>();
    public IDictionary<string, QuerySchemasSchemaPropertyResponse> Properties { get; set; } = new Dictionary<string, QuerySchemasSchemaPropertyResponse>();
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public JsonElement AliasFor { get; set; }
    
    [JsonPropertyName("$mixin")]
    public QuerySchemasSchemaMixinResponse Mixin { get; set; }
}

public class QuerySchemasSchemaPropertyResponse
{
    public string? Type { get; set; }
    
    public JsonElement Default { get; set; }
    
    public string Format { get; set; }
    
    [JsonPropertyName("$ref")]
    public string Ref { get; set; }
    
    public string AliasFor { get; set; }
    
    public QuerySchemasSchemaPropertyResponse Items { get; set; }
}

public class QuerySchemasSchemaMixinResponse
{
    [JsonPropertyName("$ref")]
    public string Ref { get; set; }
}