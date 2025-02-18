using FtrackDotNet.Models;
using Type = System.Type;

namespace FtrackDotNet.UnitOfWork;

public interface IChangeTracker
{
    void TrackEntity(object entity, Type entityType, TrackedEntityOperationType operationType);
    void OnSaved();
    Change[] GetChanges();
}