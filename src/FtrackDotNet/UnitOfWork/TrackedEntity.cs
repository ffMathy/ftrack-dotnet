using FtrackDotNet.Models;

namespace FtrackDotNet.UnitOfWork;

internal class TrackedEntity
{
    public WeakReference EntityReference { get; init; } = null!;
    public object ValueSnapshot { get; set; } = null!;
    public TrackedEntityOperationType? Operation { get; set; }
}

public enum TrackedEntityOperationType
{
    Create,
    Update,
    Delete
}