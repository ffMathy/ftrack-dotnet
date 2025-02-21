using System.Text.Json;
using FtrackDotNet.Linq;
using FtrackDotNet.UnitOfWork;

namespace FtrackDotNet.Models;

public class FtrackDataSet<T>(
    IChangeTracker changeTracker,
    IFtrackQueryProvider ftrackQueryProvider)
    : FtrackQueryable<T>(ftrackQueryProvider)
    where T : IFtrackEntity
{
    public T Add(object entity)
    {
        if (entity.GetType() != typeof(T))
        {
            throw new InvalidOperationException("Entity type does not match the dataset type.");
        }
        
        changeTracker.TrackEntity(JsonSerializer.SerializeToElement(entity, FtrackContext.GetJsonSerializerOptions()), entity, TrackedEntityOperationType.Create);
        return (T)entity;
    }

    public void Remove(object entity)
    {
        if (entity.GetType() != typeof(T))
        {
            throw new InvalidOperationException("Entity type does not match the dataset type.");
        }
        
        changeTracker.TrackEntity(JsonSerializer.SerializeToElement(entity, FtrackContext.GetJsonSerializerOptions()), entity, TrackedEntityOperationType.Delete);
    }
    
    public void Attach(object entity)
    {
        if (entity.GetType() != typeof(T))
        {
            throw new InvalidOperationException("Entity type does not match the dataset type.");
        }
        
        changeTracker.TrackEntity(JsonSerializer.SerializeToElement(entity, FtrackContext.GetJsonSerializerOptions()), entity, TrackedEntityOperationType.Update);
    }
}