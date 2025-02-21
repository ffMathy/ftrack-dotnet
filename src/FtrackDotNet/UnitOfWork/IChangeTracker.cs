using System.Text.Json;

namespace FtrackDotNet.UnitOfWork;

public interface IChangeTracker
{
    void TrackEntity(JsonElement jsonElement, object entity, TrackedEntityOperationType operationType);
    void OnSaved();
    Change[] GetChanges();
}