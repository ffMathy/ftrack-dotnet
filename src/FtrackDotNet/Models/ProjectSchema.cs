namespace FtrackDotNet.Models;

public class ProjectSchema
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ObjectType[] ObjectTypes { get; set; } = Array.Empty<ObjectType>();
    public TaskTemplate[] TaskTemplates { get; set; } = Array.Empty<TaskTemplate>();
    public ObjectTypeSchema[] ObjectTypeSchemas { get; set; } = Array.Empty<ObjectTypeSchema>();
    public AssetVersionWorkflowSchema AssetVersionWorkflowSchema { get; set; }
    public TaskTypeSchema TaskTypeSchema { get; set; }
    public TaskWorkflowSchema TaskWorkflowSchema { get; set; }
    public TaskWorkflowSchemaOverride[] TaskWorkflowSchemaOverrides { get; set; } = Array.Empty<TaskWorkflowSchemaOverride>();
}