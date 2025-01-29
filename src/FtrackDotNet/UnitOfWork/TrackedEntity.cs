namespace FtrackDotNet.UnitOfWork;

internal class TrackedEntity
{
    public WeakReference EntityReference { get; init; } = null!;
    public object ValueSnapshot { get; set; } = null!;
}