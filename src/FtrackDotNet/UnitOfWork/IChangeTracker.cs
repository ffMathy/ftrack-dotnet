using System.Text.Json;
using FtrackDotNet.Models;
using Type = System.Type;

namespace FtrackDotNet.UnitOfWork;

public interface IChangeTracker
{
    void TrackEntity(JsonElement jsonElement, object entity, TrackedEntityOperationType operationType);
    void OnSaved();
    Change[] GetChanges();
}