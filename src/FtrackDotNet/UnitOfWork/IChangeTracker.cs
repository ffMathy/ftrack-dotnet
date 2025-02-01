using FtrackDotNet.Models;

namespace FtrackDotNet.UnitOfWork;

public interface IChangeTracker
{
    void TrackEntity(object entity, TrackedEntityOperationType operationType);
    void OnSaved();
    Change[] GetChanges();
}