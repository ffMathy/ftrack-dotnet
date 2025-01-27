namespace FtrackDotNet.Models;

public class Type
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public bool IsBillable { get; set; }
    public TaskTypeSchema[] TaskTypeSchemas { get; set; } = Array.Empty<TaskTypeSchema>();
    public Task[] Tasks { get; set; } = Array.Empty<Task>();
}