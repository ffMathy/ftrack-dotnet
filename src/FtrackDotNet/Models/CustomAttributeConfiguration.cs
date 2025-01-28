using System.Text.Json;

namespace FtrackDotNet.Models;

public class CustomAttributeConfiguration
{
    public string Id { get; set; }
    public string Config { get; set; }
    public bool Core { get; set; }
    public JsonElement Default { get; set; }
    public string EntityType { get; set; }
    
    public bool IsHierarchical { get; set; }
    public string Key { get; set; }
    public string Label { get; set; }

    public ObjectType ObjectType { get; set; }
    public string ObjectTypeId { get; set; }
    
    public string ProjectId { get; set; }
}