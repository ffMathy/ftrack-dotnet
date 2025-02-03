using FtrackDotNet.Models;

namespace FtrackDotNet.UnitOfWork;

public interface IChangeTracker
{
    void TrackEntity(object entity, string entityType, TrackedEntityOperationType operationType);
    void OnSaved();
    Change[] GetChanges();
}