namespace FtrackDotNet.UnitOfWork;

internal class TrackedEntity
{
    public EntityReference Entity { get; init; }
    public IReadOnlyDictionary<string, object?> ValueSnapshot { get; set; } = null!;
    public TrackedEntityOperationType? Operation { get; set; }
}

public enum TrackedEntityOperationType
{
    Create,
    Update,
    Delete
}