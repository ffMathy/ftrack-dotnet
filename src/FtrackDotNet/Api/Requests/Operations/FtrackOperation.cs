using System.Text.Json.Serialization;
using FtrackDotNet.Api.Models;

namespace FtrackDotNet.Api.Requests.Operations;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "action")]
[JsonDerivedType(typeof(FtrackQuerySchemasOperation))]
[JsonDerivedType(typeof(FtrackQueryOperation))]
[JsonDerivedType(typeof(FtrackCreateOperation))]
[JsonDerivedType(typeof(FtrackUpdateOperation))]
[JsonDerivedType(typeof(FtrackDeleteOperation))]
public abstract class FtrackOperation
{
    [JsonPropertyName("action")]
    public abstract string Action { get; }
    
    public object Metadata { get; init; } = new();
}