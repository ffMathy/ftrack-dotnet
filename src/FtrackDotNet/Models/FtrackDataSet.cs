using System.Linq.Expressions;
using FtrackDotNet.Linq;
using FtrackDotNet.UnitOfWork;

namespace FtrackDotNet.Models;

public class FtrackDataSet<T>(
    IChangeTracker changeTracker,
    IFtrackQueryProvider ftrackQueryProvider)
    : FtrackQueryable<T>(ftrackQueryProvider)
    where T : IFtrackEntity
{
    public T Add(T entity)
    {
        changeTracker.TrackEntity(entity, typeof(T).Name, TrackedEntityOperationType.Create);
        return entity;
    }

    public void Remove(T entity)
    {
        changeTracker.TrackEntity(entity, typeof(T).Name, TrackedEntityOperationType.Delete);
    }
    
    public void Attach(T entity)
    {
        changeTracker.TrackEntity(entity, typeof(T).Name, TrackedEntityOperationType.Update);
    }
}