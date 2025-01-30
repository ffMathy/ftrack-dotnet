using FtrackDotNet.Models;

namespace FtrackDotNet.UnitOfWork;

public interface IChangeTracker
{
    void TrackEntity(IFtrackEntity entity, TrackedEntityOperationType operationType);
    void RefreshSnapshots();
}